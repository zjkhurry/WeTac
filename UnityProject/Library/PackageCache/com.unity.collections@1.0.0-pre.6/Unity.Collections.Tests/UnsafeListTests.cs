using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.Tests;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

internal class UnsafeListTests : CollectionsTestCommonBase
{
    [Test]
    public unsafe void UnsafeListT_Init_ClearMemory()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        for (var i = 0; i < list.Length; ++i)
        {
            Assert.AreEqual(0, UnsafeUtility.ReadArrayElement<int>(list.Ptr, i));
        }

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_Allocate_Deallocate_Read_Write()
    {
        var list = new UnsafeList<int>(0, Allocator.Persistent);

        list.Add(1);
        list.Add(2);

        Assert.AreEqual(2, list.Length);
        Assert.AreEqual(1, UnsafeUtility.ReadArrayElement<int>(list.Ptr, 0));
        Assert.AreEqual(2, UnsafeUtility.ReadArrayElement<int>(list.Ptr, 1));

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_Resize_ClearMemory()
    {
        var list = new UnsafeList<int>(5, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        list.SetCapacity(32);
        var capacity = list.Capacity;

        list.Resize(5, NativeArrayOptions.UninitializedMemory);
        Assert.AreEqual(capacity, list.Capacity); // list capacity should not change on resize

        for (var i = 0; i < 5; ++i)
        {
            UnsafeUtility.WriteArrayElement(list.Ptr, i, i);
        }

        list.Resize(10, NativeArrayOptions.ClearMemory);
        Assert.AreEqual(capacity, list.Capacity); // list capacity should not change on resize

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(i, UnsafeUtility.ReadArrayElement<int>(list.Ptr, i));
        }

        for (var i = 5; i < list.Length; ++i)
        {
            Assert.AreEqual(0, UnsafeUtility.ReadArrayElement<int>(list.Ptr, i));
        }

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_Resize_Zero()
    {
        var list = new UnsafeList<int>(5, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        var capacity = list.Capacity;

        list.Add(1);
        list.Resize(0);
        Assert.AreEqual(0, list.Length);
        Assert.AreEqual(capacity, list.Capacity); // list capacity should not change on resize

        list.Add(2);
        list.Clear();
        Assert.AreEqual(0, list.Length);
        Assert.AreEqual(capacity, list.Capacity); // list capacity should not change on resize

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_TrimExcess()
    {
        using (var list = new UnsafeList<int>(32, Allocator.Persistent, NativeArrayOptions.ClearMemory))
        {
            var capacity = list.Capacity;

            list.Add(1);
            list.TrimExcess();
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(1, list.Capacity);

            list.RemoveAtSwapBack(0);
            Assert.AreEqual(list.Length, 0);
            list.TrimExcess();
            Assert.AreEqual(list.Capacity, 0);

            list.Add(1);
            Assert.AreEqual(list.Length, 1);
            Assert.AreNotEqual(list.Capacity, 0);

            list.Clear();
        }
    }

    [Test]
    public unsafe void UnsafeListT_DisposeJob()
    {
        var list = new UnsafeList<int>(5, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        var disposeJob = list.Dispose(default);

        Assert.IsTrue(list.Ptr == null);

        disposeJob.Complete();
    }

    unsafe void Expected(ref UnsafeList<int> list, int expectedLength, int[] expected)
    {
        Assert.AreEqual(0 == expectedLength, list.IsEmpty);
        Assert.AreEqual(list.Length, expectedLength);
        for (var i = 0; i < list.Length; ++i)
        {
            var value = UnsafeUtility.ReadArrayElement<int>(list.Ptr, i);
            Assert.AreEqual(expected[i], value);
        }
    }

    [Test]
    public unsafe void UnsafeListT_AddNoResize()
    {
        var list = new UnsafeList<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // List's capacity is always cache-line aligned, number of items fills up whole cache-line.
        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        Assert.Throws<Exception>(() => { fixed (int* r = range) list.AddRangeNoResize(r, 17); });

        list.SetCapacity(17);
        Assert.DoesNotThrow(() => { fixed (int* r = range) list.AddRangeNoResize(r, 17); });

        list.SetCapacity(16);
        Assert.Throws<Exception>(() => { list.AddNoResize(16); });
    }

    [Test]
    public unsafe void UnsafeListT_AddNoResize_Read()
    {
        var list = new UnsafeList<int>(4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        list.AddNoResize(4);
        list.AddNoResize(6);
        list.AddNoResize(4);
        list.AddNoResize(9);
        Expected(ref list, 4, new int[] { 4, 6, 4, 9 });

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_RemoveAtSwapBack()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveAtSwapBack(list.Length - 1);
        Expected(ref list, 9, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
        list.Clear();

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveAtSwapBack(5);
        Expected(ref list, 9, new int[] { 0, 1, 2, 3, 4, 9, 6, 7, 8 });
        list.Clear();

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_RemoveRangeSwapBackBE()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRangeSwapBack(6, 3);
        Expected(ref list, 7, new int[] { 0, 1, 2, 3, 4, 5, 9 });
        list.Clear();

        // test removing all but one
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRangeSwapBack(0, 9);
        Expected(ref list, 1, new int[] { 9 });
        list.Clear();

        // test removing from the front
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRangeSwapBack(0, 3);
        Expected(ref list, 7, new int[] { 7, 8, 9, 3, 4, 5, 6 });
        list.Clear();

        // test removing from the middle
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRangeSwapBack(0, 3);
        Expected(ref list, 7, new int[] { 7, 8, 9, 3, 4, 5, 6 });
        list.Clear();

        // test removing whole range
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRangeSwapBack(0, 10);
        Expected(ref list, 0, new int[] { 0 });
        list.Clear();

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_RemoveAt()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveAt(list.Length - 1);
        Expected(ref list, 9, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
        list.Clear();

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveAt(5);
        Expected(ref list, 9, new int[] { 0, 1, 2, 3, 4, 6, 7, 8, 9 });
        list.Clear();

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_RemoveRange()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // test removing from the end
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRange(6, 3);
        Expected(ref list, 7, new int[] { 0, 1, 2, 3, 4, 5, 9 });
        list.Clear();

        // test removing all but one
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRange(0, 9);
        Expected(ref list, 1, new int[] { 9 });
        list.Clear();

        // test removing from the front
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRange(0, 3);
        Expected(ref list, 7, new int[] { 3, 4, 5, 6, 7, 8, 9 });
        list.Clear();

        // test removing from the middle
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRange(0, 3);
        Expected(ref list, 7, new int[] { 3, 4, 5, 6, 7, 8, 9 });
        list.Clear();

        // test removing whole range
        fixed (int* r = range) list.AddRange(r, 10);
        list.RemoveRange(0, 10);
        Expected(ref list, 0, new int[] { 0 });
        list.Clear();

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_Remove_Throws()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        fixed (int* r = range) list.AddRange(r, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveAt(100); });
        Assert.AreEqual(10, list.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveAtSwapBack(100); });
        Assert.AreEqual(10, list.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveRange(0, 100); });
        Assert.AreEqual(10, list.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveRangeSwapBack(0, 100); });
        Assert.AreEqual(10, list.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveRange(100, -1); });
        Assert.AreEqual(10, list.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list.RemoveRangeSwapBack(100, -1); });
        Assert.AreEqual(10, list.Length);

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_PtrLength()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int[] range = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        fixed (int* r = range) list.AddRange(r, 10);

        var listView = new UnsafeList<int>(list.Ptr + 4, 2);
        Expected(ref listView, 2, new int[] { 4, 5 });

        listView.Dispose();
        list.Dispose();
    }

    // Burst error BC1071: Unsupported assert type
    // [BurstCompile(CompileSynchronously = true)]
    struct UnsafeListParallelReader : IJob
    {
        public UnsafeList<int>.ParallelReader list;

        public void Execute()
        {
            Assert.True(list.Contains(123));
        }
    }

    [Test]
    public void UnsafeListT_ParallelReader()
    {
        var list = new UnsafeList<int>(10, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        list.Add(123);

        var job = new UnsafeListParallelReader
        {
            list = list.AsParallelReader(),
        };

        list.Dispose(job.Schedule()).Complete();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct UnsafeListParallelWriter : IJobParallelFor
    {
        public UnsafeList<int>.ParallelWriter list;

        public void Execute(int index)
        {
            list.AddNoResize(index);
        }
    }

    [Test]
    public void UnsafeListT_ParallelWriter()
    {
        var list = new UnsafeList<int>(256, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        var job = new UnsafeListParallelWriter
        {
            list = list.AsParallelWriter(),
        };

        job.Schedule(list.Capacity, 1).Complete();

        Assert.AreEqual(list.Length, list.Capacity);

        list.Sort<int>();

        for (int i = 0; i < list.Length; i++)
        {
            unsafe
            {
                var value = UnsafeUtility.ReadArrayElement<int>(list.Ptr, i);
                Assert.AreEqual(i, value);
            }
        }

        list.Dispose();
    }

    [Test]
    public unsafe void UnsafeListT_IndexOf()
    {
        using (var list = new UnsafeList<int>(10, Allocator.Persistent) { 123, 789 })
        {
            bool r0 = false, r1 = false, r2 = false;

            GCAllocRecorder.ValidateNoGCAllocs(() =>
            {
                r0 = -1 != list.IndexOf(456);
                r1 = list.Contains(123);
                r2 = list.Contains(789);
            });

            Assert.False(r0);
            Assert.True(r1);
            Assert.True(r2);
        }
    }

    [Test]
    public void UnsafeListT_InsertRangeWithBeginEnd()
    {
        var list = new UnsafeList<byte>(3, Allocator.Persistent);
        list.Add(0);
        list.Add(3);
        list.Add(4);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRangeWithBeginEnd(-1, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRangeWithBeginEnd(0, 8));
        Assert.Throws<ArgumentException>(() => list.InsertRangeWithBeginEnd(3, 1));

        Assert.DoesNotThrow(() => list.InsertRangeWithBeginEnd(1, 3));

        list[1] = 1;
        list[2] = 2;

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(i, list[i]);
        }

        list.Dispose();
    }

    [Test]
    public void UnsafeListT_ForEach([Values(10, 1000)]int n)
    {
        var seen = new NativeArray<int>(n, Allocator.Temp);
        using (var container = new UnsafeList<int>(32, Allocator.TempJob))
        {
            for (int i = 0; i < n; i++)
            {
                container.Add(i);
            }

            var count = 0;
            unsafe
            {
                UnsafeList<int>* test = &container;

                foreach (var item in *test)
                {
                    Assert.True(test->Contains(item));
                    seen[item] = seen[item] + 1;
                    ++count;
                }
            }

            Assert.AreEqual(container.Length, count);
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(1, seen[i], $"Incorrect item count {i}");
            }
        }
    }

    [Test]
    public void UnsafeList_CustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        using (var container = new UnsafeList<byte>(1, allocator.Handle))
        {
        }

        Assert.IsTrue(allocator.WasUsed);
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }

    [BurstCompile]
    struct BurstedCustomAllocatorJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe CustomAllocatorTests.CountingAllocator* Allocator;

        public void Execute()
        {
            unsafe
            {
                using (var container = new UnsafeList<byte>(1, Allocator->Handle))
                {
                }
            }
        }
    }

    [Test]
    public void UnsafeList_BurstedCustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        unsafe
        {
            var handle = new BurstedCustomAllocatorJob {Allocator = &allocator}.Schedule();
            handle.Complete();
        }

        Assert.IsTrue(allocator.WasUsed);
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }
}
