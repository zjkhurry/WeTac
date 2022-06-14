using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Doc.CodeSamples.Collections.Tests
{
    struct ExamplesCollections
    {
        public void foo()
        {
            #region parallel_writer

            NativeList<int> nums = new NativeList<int>(1000, Allocator.TempJob);

            // The parallel writer shares the original list's AtomicSafetyHandle.
            var job = new MyParallelJob {NumsWriter = nums.AsParallelWriter()};

            #endregion
        }

        #region parallel_writer_job

        public struct MyParallelJob : IJobParallelFor
        {
            public NativeList<int>.ParallelWriter NumsWriter;

            public void Execute(int i)
            {
                // A NativeList<T>.ParallelWriter can append values
                // but not grow the capacity of the list.
                NumsWriter.AddNoResize(i);
            }
        }

        #endregion

        public void foo2()
        {
            #region enumerator
            NativeList<int> nums = new NativeList<int>(10, Allocator.Temp);

            // Calculate the sum of all elements in the list.
            int sum = 0;
            NativeArray<int>.Enumerator enumerator = nums.GetEnumerator();

            // The first MoveNext call advances the enumerator to the first element.
            // MoveNext returns false when the enumerator has advanced past the last element.
            while (enumerator.MoveNext())
            {
                sum += enumerator.Current;
            }

            // The enumerator is no longer valid to use after the array is disposed.
            nums.Dispose();
            #endregion
        }

        #region read_only
        public struct MyJob : IJob
        {
            // This array can only be read in the job.
            [ReadOnly] public NativeArray<int> nums;

            public void Execute()
            {
                // If safety checks are enabled, an exception is thrown here
                // because the array is read only.
                nums[0] = 100;
            }
        }
        #endregion
    }

    #region collections_deterministic_sort

    internal partial class MySystemCollections : SystemBase
    {
        protected override void OnUpdate()
        {
            // This artificial example copies all Foo component values to a
            // list in parallel, then sorts the list based on entityInQueryIndex
            // to make the order in the list deterministic.

            // For simplicity, we'll assume we know that there are no
            // more than 100 entities with a Foo component.
            NativeList<SortableFoo> list = new NativeList<SortableFoo>(100, Allocator.TempJob);
            NativeList<SortableFoo>.ParallelWriter writer = list.AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, in Foo foo) =>
            {
                writer.AddNoResize(
                    new SortableFoo {SortKey = entityInQueryIndex, Foo = foo}
                );
            }).ScheduleParallel();
            Dependency.Complete(); // Completes the job.

            // Because the sort criteria does not depend upon scheduling happenstance,
            // the resulting order is deterministic after the sort.
            list.Sort(new SortableFooComparer());

            // ... Use the sorted list.
        }
    }

    internal struct SortableFoo
    {
        public int SortKey;
        public Foo Foo;
    }

    internal struct SortableFooComparer : IComparer<SortableFoo>
    {
        public int Compare(SortableFoo x, SortableFoo y)
        {
            if (x.SortKey == y.SortKey)
                return 0;
            return (x.SortKey == y.SortKey) ? -1 : 1;
        }
    }

    #endregion

    internal struct Foo : IComponentData
    {
        public int Value;
    }
}
