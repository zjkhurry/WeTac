using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.NotBurstCompatible;
using Unity.Collections.Tests;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
#if !UNITY_PORTABLE_TEST_RUNNER
using System.Text.RegularExpressions;
#endif

internal class NativeMultiHashMapTests : CollectionsTestFixture
{
    // These tests require:
    // - JobsDebugger support for static safety IDs (added in 2020.1)
    // - Asserting throws
#if !UNITY_DOTSRUNTIME
    [Test, DotsRuntimeIgnore]
    public void NativeMultiHashMap_UseAfterFree_UsesCustomOwnerTypeName()
    {
        var container = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);
        container.Add(0, 123);
        container.Dispose();
        Assert.That(() => container.ContainsKey(0),
            Throws.Exception.TypeOf<ObjectDisposedException>()
                .With.Message.Contains($"The {container.GetType()} has been deallocated"));
    }

    [BurstCompile(CompileSynchronously = true)]
    struct NativeMultiHashMap_CreateAndUseAfterFreeBurst : IJob
    {
        public void Execute()
        {
            var container = new NativeMultiHashMap<int, int>(10, Allocator.Temp);
            container.Add(0, 17);
            container.Dispose();
            container.Add(1, 42);
        }
    }

    [Test, DotsRuntimeIgnore]
    public void NativeMultiHashMap_CreateAndUseAfterFreeInBurstJob_UsesCustomOwnerTypeName()
    {
        // Make sure this isn't the first container of this type ever created, so that valid static safety data exists
        var container = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);
        container.Dispose();

        var job = new NativeMultiHashMap_CreateAndUseAfterFreeBurst
        {
        };

        // Two things:
        // 1. This exception is logged, not thrown; thus, we use LogAssert to detect it.
        // 2. Calling write operation after container.Dispose() emits an unintuitive error message. For now, all this test cares about is whether it contains the
        //    expected type name.
        job.Run();
        LogAssert.Expect(LogType.Exception,
            new Regex($"InvalidOperationException: The {Regex.Escape(container.GetType().ToString())} has been declared as \\[ReadOnly\\] in the job, but you are writing to it"));
    }
#endif

    [Test]
    public void NativeMultiHashMap_IsEmpty()
    {
        var container = new NativeMultiHashMap<int, int>(0, Allocator.Persistent);
        Assert.IsTrue(container.IsEmpty);

        container.Add(0, 0);
        Assert.IsFalse(container.IsEmpty);
        Assert.AreEqual(1, container.Capacity);
        ExpectedCount(ref container, 1);

        container.Remove(0, 0);
        Assert.IsTrue(container.IsEmpty);

        container.Add(0, 0);
        container.Clear();
        Assert.IsTrue(container.IsEmpty);

        container.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_CountValuesForKey()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        hashMap.Add(5, 7);
        hashMap.Add(6, 9);
        hashMap.Add(6, 10);

        Assert.AreEqual(1, hashMap.CountValuesForKey(5));
        Assert.AreEqual(2, hashMap.CountValuesForKey(6));
        Assert.AreEqual(0, hashMap.CountValuesForKey(7));

        hashMap.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_RemoveKeyAndValue()
    {
        var hashMap = new NativeMultiHashMap<int, long>(1, Allocator.Temp);
        hashMap.Add(10, 0);
        hashMap.Add(10, 1);
        hashMap.Add(10, 2);

        hashMap.Add(20, 2);
        hashMap.Add(20, 2);
        hashMap.Add(20, 1);
        hashMap.Add(20, 2);
        hashMap.Add(20, 1);

        hashMap.Remove(10, 1L);
        ExpectValues(hashMap, 10, new[] { 0L, 2L });
        ExpectValues(hashMap, 20, new[] { 1L, 1L, 2L, 2L, 2L });

        hashMap.Remove(20, 2L);
        ExpectValues(hashMap, 10, new[] { 0L, 2L });
        ExpectValues(hashMap, 20, new[] { 1L, 1L });

        hashMap.Remove(20, 1L);
        ExpectValues(hashMap, 10, new[] { 0L, 2L });
        ExpectValues(hashMap, 20, new long[0]);

        hashMap.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_ValueIterator()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        hashMap.Add(5, 0);
        hashMap.Add(5, 1);
        hashMap.Add(5, 2);

        var list = new NativeList<int>(Allocator.TempJob);

        GCAllocRecorder.ValidateNoGCAllocs(() =>
        {
            list.Clear();
            foreach (var value in hashMap.GetValuesForKey(5))
                list.Add(value);
        });

        list.Sort();
        Assert.AreEqual(list.ToArrayNBC(), new int[] { 0, 1, 2 });

        foreach (var value in hashMap.GetValuesForKey(6))
            Assert.Fail();

        list.Dispose();
        hashMap.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_RemoveKeyValueDoesntDeallocate()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp) { { 5, 1 } };

        hashMap.Remove(5, 5);
        GCAllocRecorder.ValidateNoGCAllocs(() =>
        {
            hashMap.Remove(5, 1);
        });
        Assert.IsTrue(hashMap.IsEmpty);

        hashMap.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_Double_Deallocate_Throws()
    {
        var hashMap = new NativeMultiHashMap<int, int>(16, Allocator.TempJob);
        hashMap.Dispose();
        Assert.Throws<ObjectDisposedException>(
            () => { hashMap.Dispose(); });
    }

    static void ExpectedCount<TKey, TValue>(ref NativeMultiHashMap<TKey, TValue> container, int expected)
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        Assert.AreEqual(expected == 0, container.IsEmpty);
        Assert.AreEqual(expected, container.Count());
    }

    [Test]
    public void NativeMultiHashMap_RemoveOnEmptyMap_DoesNotThrow()
    {
        var hashMap = new NativeMultiHashMap<int, int>(0, Allocator.Temp);

        Assert.DoesNotThrow(() => hashMap.Remove(0));
        Assert.DoesNotThrow(() => hashMap.Remove(-425196));
        Assert.DoesNotThrow(() => hashMap.Remove(0, 0));
        Assert.DoesNotThrow(() => hashMap.Remove(-425196, 0));

        hashMap.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_RemoveFromMultiHashMap()
    {
        var hashMap = new NativeMultiHashMap<int, int>(16, Allocator.Temp);
        int iSquared;
        // Make sure inserting values work
        for (int i = 0; i < 8; ++i)
            hashMap.Add(i, i * i);
        for (int i = 0; i < 8; ++i)
            hashMap.Add(i, i);
        Assert.AreEqual(16, hashMap.Capacity, "HashMap grew larger than expected");
        // Make sure reading the inserted values work
        for (int i = 0; i < 8; ++i)
        {
            NativeMultiHashMapIterator<int> it;
            Assert.IsTrue(hashMap.TryGetFirstValue(i, out iSquared, out it), "Failed get value from hash table");
            Assert.AreEqual(iSquared, i, "Got the wrong value from the hash table");
            Assert.IsTrue(hashMap.TryGetNextValue(out iSquared, ref it), "Failed get value from hash table");
            Assert.AreEqual(iSquared, i * i, "Got the wrong value from the hash table");
        }
        for (int rm = 0; rm < 8; ++rm)
        {
            Assert.AreEqual(2, hashMap.Remove(rm));
            NativeMultiHashMapIterator<int> it;
            Assert.IsFalse(hashMap.TryGetFirstValue(rm, out iSquared, out it), "Failed to remove value from hash table");
            for (int i = rm + 1; i < 8; ++i)
            {
                Assert.IsTrue(hashMap.TryGetFirstValue(i, out iSquared, out it), "Failed get value from hash table");
                Assert.AreEqual(iSquared, i, "Got the wrong value from the hash table");
                Assert.IsTrue(hashMap.TryGetNextValue(out iSquared, ref it), "Failed get value from hash table");
                Assert.AreEqual(iSquared, i * i, "Got the wrong value from the hash table");
            }
        }
        // Make sure entries were freed
        for (int i = 0; i < 8; ++i)
            hashMap.Add(i, i * i);
        for (int i = 0; i < 8; ++i)
            hashMap.Add(i, i);
        Assert.AreEqual(16, hashMap.Capacity, "HashMap grew larger than expected");
        hashMap.Dispose();
    }

    void ExpectValues(NativeMultiHashMap<int, long> hashMap, int key, long[] expectedValues)
    {
        var list = new NativeList<long>(Allocator.TempJob);
        foreach (var value in hashMap.GetValuesForKey(key))
            list.Add(value);

        list.Sort();
        Assert.AreEqual(list.ToArrayNBC(), expectedValues);
        list.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_GetKeys()
    {
        var container = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        for (int i = 0; i < 30; ++i)
        {
            container.Add(i, 2 * i);
            container.Add(i, 3 * i);
        }
        var keys = container.GetKeyArray(Allocator.Temp);
#if !NET_DOTS // Tuple is not supported by TinyBCL
        var (unique, uniqueLength) = container.GetUniqueKeyArrayNBC(Allocator.Temp);
        Assert.AreEqual(30, uniqueLength);
#endif

        Assert.AreEqual(60, keys.Length);
        keys.Sort();
        for (int i = 0; i < 30; ++i)
        {
            Assert.AreEqual(i, keys[i * 2 + 0]);
            Assert.AreEqual(i, keys[i * 2 + 1]);
#if !NET_DOTS // Tuple is not supported by TinyBCL
            Assert.AreEqual(i, unique[i]);
#endif
        }
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void NativeMultiHashMap_GetUniqueKeysEmpty()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        var keys = hashMap.GetUniqueKeyArrayNBC(Allocator.Temp);

        Assert.AreEqual(0, keys.Item1.Length);
        Assert.AreEqual(0, keys.Item2);
    }

    [Test]
    public void NativeMultiHashMap_GetUniqueKeys()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        for (int i = 0; i < 30; ++i)
        {
            hashMap.Add(i, 2 * i);
            hashMap.Add(i, 3 * i);
        }
        var keys = hashMap.GetUniqueKeyArrayNBC(Allocator.Temp);
        hashMap.Dispose();
        Assert.AreEqual(30, keys.Item2);
        for (int i = 0; i < 30; ++i)
        {
            Assert.AreEqual(i, keys.Item1[i]);
        }
        keys.Item1.Dispose();
    }

#endif

    [Test]
    public void NativeMultiHashMap_GetValues()
    {
        var hashMap = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        for (int i = 0; i < 30; ++i)
        {
            hashMap.Add(i, 30 + i);
            hashMap.Add(i, 60 + i);
        }
        var values = hashMap.GetValueArray(Allocator.Temp);
        hashMap.Dispose();

        Assert.AreEqual(60, values.Length);
        values.Sort();
        for (int i = 0; i < 60; ++i)
        {
            Assert.AreEqual(30 + i, values[i]);
        }
        values.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_ForEach_FixedStringInHashMap()
    {
        using (var stringList = new NativeList<FixedString32Bytes>(10, Allocator.Persistent) { "Hello", ",", "World", "!" })
        {
            var container = new NativeMultiHashMap<FixedString128Bytes, float>(50, Allocator.Temp);
            var seen = new NativeArray<int>(stringList.Length, Allocator.Temp);
            foreach (var str in stringList)
            {
                container.Add(str, 0);
            }

            foreach (var pair in container)
            {
                int index = stringList.IndexOf(pair.Key);
                Assert.AreEqual(stringList[index], pair.Key.ToString());
                seen[index] = seen[index] + 1;
            }

            for (int i = 0; i < stringList.Length; i++)
            {
                Assert.AreEqual(1, seen[i], $"Incorrect value count {stringList[i]}");
            }
        }
    }

    [Test]
    public void NativeMultiHashMap_ForEach([Values(10, 1000)]int n)
    {
        var seenKeys = new NativeArray<int>(n, Allocator.Temp);
        var seenValues = new NativeArray<int>(n * 2, Allocator.Temp);
        using (var container = new NativeMultiHashMap<int, int>(1, Allocator.Temp))
        {
            for (int i = 0; i < n; ++i)
            {
                container.Add(i, i);
                container.Add(i, i + n);
            }

            var count = 0;
            foreach (var kv in container)
            {
                if (kv.Value < n)
                {
                    Assert.AreEqual(kv.Key, kv.Value);
                }
                else
                {
                    Assert.AreEqual(kv.Key + n, kv.Value);
                }

                seenKeys[kv.Key] = seenKeys[kv.Key] + 1;
                seenValues[kv.Value] = seenValues[kv.Value] + 1;

                ++count;
            }

            Assert.AreEqual(container.Count(), count);
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(2, seenKeys[i], $"Incorrect key count {i}");
                Assert.AreEqual(1, seenValues[i], $"Incorrect value count {i}");
                Assert.AreEqual(1, seenValues[i + n], $"Incorrect value count {i + n}");
            }
        }
    }

    [Test]
    public void NativeMultiHashMap_ForEach_Throws_When_Modified()
    {
        using (var container = new NativeMultiHashMap<int, int>(32, Allocator.TempJob))
        {
            for (int i = 0; i < 30; ++i)
            {
                container.Add(i, 30 + i);
                container.Add(i, 60 + i);
            }

            Assert.Throws<ObjectDisposedException>(() =>
            {
                foreach (var kv in container)
                {
                    container.Add(10, 10);
                }
            });

            Assert.Throws<ObjectDisposedException>(() =>
            {
                foreach (var kv in container)
                {
                    container.Remove(1);
                }
            });
        }
    }

    struct NativeMultiHashMap_ForEachIterator : IJob
    {
        [ReadOnly]
        public NativeMultiHashMap<int, int>.KeyValueEnumerator Iter;

        public void Execute()
        {
            while (Iter.MoveNext())
            {
            }
        }
    }

    [Test]
    public void NativeMultiHashMap_ForEach_Throws_Job_Iterator()
    {
        using (var container = new NativeMultiHashMap<int, int>(32, Allocator.TempJob))
        {
            var jobHandle = new NativeMultiHashMap_ForEachIterator
            {
                Iter = container.GetEnumerator()

            }.Schedule();

            Assert.Throws<InvalidOperationException>(() => { container.Add(1, 1); });

            jobHandle.Complete();
        }
    }

    struct ParallelWriteToMultiHashMapJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeMultiHashMap<int, int>.ParallelWriter Writer;

        public void Execute(int index)
        {
            Writer.Add(index, 0);
        }
    }

    [Test]
    public void NativeMultiHashMap_ForEach_Throws_When_Modified_From_Job()
    {
        using (var container = new NativeMultiHashMap<int, int>(32, Allocator.TempJob))
        {
            var iter = container.GetEnumerator();

            var jobHandle = new ParallelWriteToMultiHashMapJob
            {
                Writer = container.AsParallelWriter()

            }.Schedule(1, 2);

            Assert.Throws<ObjectDisposedException>(() =>
            {
                while (iter.MoveNext())
                {
                }
            });

            jobHandle.Complete();
        }
    }

#if !UNITY_PORTABLE_TEST_RUNNER    // https://unity3d.atlassian.net/browse/DOTSR-1432
    [Test]
    public void NativeMultiHashMap_GetKeysAndValues()
    {
        var container = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        for (int i = 0; i < 30; ++i)
        {
            container.Add(i, 30 + i);
            container.Add(i, 60 + i);
        }
        var keysValues = container.GetKeyValueArrays(Allocator.Temp);
        container.Dispose();

        Assert.AreEqual(60, keysValues.Keys.Length);
        Assert.AreEqual(60, keysValues.Values.Length);

        // ensure keys and matching values are aligned (though unordered)
        for (int i = 0; i < 30; ++i)
        {
            var k0 = keysValues.Keys[i * 2 + 0];
            var k1 = keysValues.Keys[i * 2 + 1];
            var v0 = keysValues.Values[i * 2 + 0];
            var v1 = keysValues.Values[i * 2 + 1];

            if (v0 > v1)
                (v0, v1) = (v1, v0);

            Assert.AreEqual(k0, k1);
            Assert.AreEqual(30 + k0, v0);
            Assert.AreEqual(60 + k0, v1);
        }

        keysValues.Keys.Sort();
        for (int i = 0; i < 30; ++i)
        {
            Assert.AreEqual(i, keysValues.Keys[i * 2 + 0]);
            Assert.AreEqual(i, keysValues.Keys[i * 2 + 1]);
        }

        keysValues.Values.Sort();
        for (int i = 0; i < 60; ++i)
        {
            Assert.AreEqual(30 + i, keysValues.Values[i]);
        }

        keysValues.Dispose();
    }
#endif

    [Test]
    public void NativeMultiHashMap_ContainsKeyMultiHashMap()
    {
        var container = new NativeMultiHashMap<int, int>(1, Allocator.Temp);
        container.Add(5, 7);

        container.Add(6, 9);
        container.Add(6, 10);

        Assert.IsTrue(container.ContainsKey(5));
        Assert.IsTrue(container.ContainsKey(6));
        Assert.IsFalse(container.ContainsKey(4));

        container.Dispose();
    }

    [Test]
    public void NativeMultiHashMap_CustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        using (var container = new NativeMultiHashMap<int, int>(1, allocator.Handle))
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
                using (var container = new NativeMultiHashMap<int, int>(1, Allocator->Handle))
                {
                }
            }
        }
    }

    [Test]
    public void NativeMultiHashMap_BurstedCustomAllocatorTest()
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
