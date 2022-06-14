using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;
using Unity.Jobs;

class NativeReferenceTests : CollectionsTestCommonBase
{
    [Test]
    public void NativeReference_AllocateDeallocate_ReadWrite()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        reference.Value = 1;

        Assert.That(reference.Value, Is.EqualTo(1));

        reference.Dispose();
    }

    [Test]
    public void NativeReference_CopyFrom()
    {
        var referenceA = new NativeReference<TestData>(Allocator.Persistent);
        var referenceB = new NativeReference<TestData>(Allocator.Persistent);

        referenceA.Value = new TestData { Integer = 42, Float = 3.1416f };
        referenceB.CopyFrom(referenceA);

        Assert.That(referenceB.Value, Is.EqualTo(referenceA.Value));

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_CopyTo()
    {
        var referenceA = new NativeReference<TestData>(Allocator.Persistent);
        var referenceB = new NativeReference<TestData>(Allocator.Persistent);

        referenceA.Value = new TestData { Integer = 42, Float = 3.1416f };
        referenceA.CopyTo(referenceB);

        Assert.That(referenceB.Value, Is.EqualTo(referenceA.Value));

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_NullThrows()
    {
        var reference = new NativeReference<int>();
        Assert.Throws<NullReferenceException>(() => reference.Value = 5);
    }

    [Test]
    public void NativeReference_CopiedIsKeptInSync()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        var referenceCopy = reference;
        reference.Value = 42;

        Assert.That(reference.Value, Is.EqualTo(referenceCopy.Value));

        reference.Dispose();
    }

    struct TestData
    {
        public int Integer;
        public float Float;
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TempNativeReferenceInJob : IJob
    {
        public NativeReference<int> Output;

        public void Execute()
        {
            var reference = new NativeReference<int>(Allocator.Temp);
            reference.Value = 42;
            Output.Value = reference.Value;
            reference.Dispose();
        }
    }

    [Test]
    public void NativeReference_TempInBurstJob()
    {
        var job = new TempNativeReferenceInJob() { Output = new NativeReference<int>(Allocator.TempJob) };
        job.Schedule().Complete();

        Assert.That(job.Output.Value, Is.EqualTo(42));

        job.Output.Dispose();
    }

    [Test]
    public unsafe void NativeReference_UnsafePtr()
    {
        var reference = new NativeReference<int>(Allocator.TempJob);
        var job = new TempNativeReferenceInJob() { Output = reference };
        var jobHandle = job.Schedule();

        Assert.Throws<InvalidOperationException>(() => reference.GetUnsafePtr());
        Assert.Throws<InvalidOperationException>(() => reference.GetUnsafeReadOnlyPtr());
        Assert.DoesNotThrow(() => reference.GetUnsafePtrWithoutChecks());

        jobHandle.Complete();

        Assert.AreEqual(*(int*)reference.GetUnsafePtr(), 42);
        Assert.AreEqual(*(int*)reference.GetUnsafeReadOnlyPtr(), 42);
        Assert.AreEqual(*(int*)reference.GetUnsafePtrWithoutChecks(), 42);

        Assert.That(job.Output.Value, Is.EqualTo(42));

        job.Output.Dispose();
    }

    [Test]
    public void NativeReference_DisposeJob()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        Assert.That(reference.IsCreated, Is.True);
        Assert.DoesNotThrow(() => reference.Value = 99);

        var disposeJob = reference.Dispose(default);
        Assert.That(reference.IsCreated, Is.False);

        Assert.Throws<ObjectDisposedException>(() => reference.Value = 3);

        disposeJob.Complete();
    }

    [Test]
    public void NativeReference_NoGCAllocations()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);

        GCAllocRecorder.ValidateNoGCAllocs(() =>
        {
            reference.Value = 1;
            reference.Value++;
        });

        Assert.That(reference.Value, Is.EqualTo(2));

        reference.Dispose();
    }

    [Test]
    public void NativeReference_Equals()
    {
        var referenceA = new NativeReference<int>(12345, Allocator.Persistent);
        var referenceB = new NativeReference<int>(Allocator.Persistent) { Value = 12345 };
        Assert.That(referenceA, Is.EqualTo(referenceB));

        referenceB.Value = 54321;
        Assert.AreNotEqual(referenceA, referenceB);

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_ReadOnly()
    {
        var referenceA = new NativeReference<int>(12345, Allocator.Persistent);
        var referenceB = new NativeReference<int>(Allocator.Persistent) { Value = 12345 };

        var referenceARO = referenceA.AsReadOnly();
        Assert.AreEqual(referenceARO.Value, referenceB.Value);

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_GetHashCode()
    {
        var integer = 42;
        var reference = new NativeReference<int>(integer, Allocator.Persistent);
        Assert.That(reference.GetHashCode(), Is.EqualTo(integer.GetHashCode()));

        reference.Dispose();
    }

    [Test]
    public void NativeReference_CustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        using (var container = new NativeReference<int>(allocator.Handle))
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
                using (var container = new NativeReference<int>(Allocator->Handle))
                {
                }
            }
        }
    }

    [Test]
    public void NativeReference_BurstedCustomAllocatorTest()
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
