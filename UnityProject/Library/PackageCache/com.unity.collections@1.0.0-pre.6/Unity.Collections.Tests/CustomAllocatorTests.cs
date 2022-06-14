using System;
using System.Runtime.InteropServices;
using AOT;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

internal class CustomAllocatorTests
{

#if !NET_DOTS
    [TestCase(1337,   1337)]
    [TestCase(0xFFFF, 0x0000)]
    [TestCase(0x0000, 0xFFFF)]
    [TestCase(0xFFFF, 0xFFFF)]
    [TestCase(0x0000, 0x0000)]
    public void AllocatorHandleToAllocatorRoundTripWorks(int i, int v)
    {
        var Index = (ushort)i;
        var Version = (ushort)v;
        AllocatorManager.AllocatorHandle srcHandle = new AllocatorManager.AllocatorHandle{ Index = Index, Version = Version };
        Allocator srcAllocator = srcHandle.ToAllocator;
        AllocatorManager.AllocatorHandle destHandle = srcAllocator;
        Assert.AreEqual(srcHandle.Index, destHandle.Index);
        Assert.AreEqual(srcHandle.Version, destHandle.Version);
        Allocator destAllocator = destHandle.ToAllocator;
        Assert.AreEqual(srcAllocator, destAllocator);
    }
#endif

    [Test]
    public void AllocatorVersioningWorks()
    {
        AllocatorManager.Initialize();
        var origin = AllocatorManager.Persistent;
        var storage = origin.AllocateBlock(default(byte), 100000); // allocate a block of bytes from Malloc.Persistent
        for(var i = 1; i <= 3; ++i)
        {
            AllocatorManager.StackAllocator allocator = default;
            allocator.Initialize(storage);
            var oldIndex = allocator.Handle.Index;
            var oldVersion = allocator.Handle.Version;
            allocator.Dispose();
            var newVersion = AllocatorManager.SharedStatics.Version.Ref.Data.ElementAt(oldIndex);
            Assert.AreEqual(oldVersion + 1, newVersion);
        }
        storage.Dispose();
        AllocatorManager.Shutdown();
    }


#if !UNITY_DOTSRUNTIME
    [Test]
    public void ReleasingChildHandlesWorks()
    {
        AllocatorManager.Initialize();
        var origin = AllocatorManager.Persistent;
        var storage = origin.AllocateBlock(default(byte), 100000); // allocate a block of bytes from Malloc.Persistent
        AllocatorManager.StackAllocator allocator = default;
        allocator.Initialize(storage);
        var list = NativeList<int>.New(10, ref allocator);
        list.Add(0); // put something in the list, so it'll have a size for later
        allocator.Dispose(); // ok to tear down the storage that the stack allocator used, too.
        Assert.Throws<ObjectDisposedException>(
        () => {
            list[0] = 0; // we haven't disposed this list, but it was released automatically already. so this is an error.
        });
        storage.Dispose();
        AllocatorManager.Shutdown();
    }

    [Test]
    public unsafe void ReleasingChildAllocatorsWorks()
    {
        AllocatorManager.Initialize();

        var origin = AllocatorManager.Persistent;
        var parentStorage = origin.AllocateBlock(default(byte), 100000); // allocate a block of bytes from Malloc.Persistent
        AllocatorManager.StackAllocator parent = default;
        parent.Initialize(parentStorage);  // and make a stack allocator from it

        var childStorage = parent.AllocateBlock(default(byte), 10000); // allocate some space from the parent
        AllocatorManager.StackAllocator child = default;
        child.Initialize(childStorage);  // and make a stack allocator from it

        parent.Dispose(); // tear down the parent allocator

        Assert.Throws<ArgumentException>(() =>
        {
            child.Allocate(default(byte), 1000); // try to allocate from the child - it should fail.
        });

        parentStorage.Dispose();

        AllocatorManager.Shutdown();
    }
#endif

    [Test]
    public void AllocatesAndFreesFromMono()
    {
        AllocatorManager.Initialize();
        const int kLength = 100;
        for (int i = 0; i < kLength; ++i)
        {
            var allocator = AllocatorManager.Persistent;
            var block = allocator.AllocateBlock(default(int), i);
            if(i != 0)
                Assert.AreNotEqual(IntPtr.Zero, block.Range.Pointer);
            Assert.AreEqual(i, block.Range.Items);
            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block.BytesPerItem);
            Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block.Alignment);
            Assert.AreEqual(AllocatorManager.Persistent.Value, block.Range.Allocator.Value);
            allocator.FreeBlock(ref block);
        }
        AllocatorManager.Shutdown();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct AllocateJob : IJobParallelFor
    {
        public NativeArray<AllocatorManager.Block> m_blocks;
        public void Execute(int index)
        {
            var allocator = AllocatorManager.Persistent;
            m_blocks[index] = allocator.AllocateBlock(default(int), index);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct FreeJob : IJobParallelFor
    {
        public NativeArray<AllocatorManager.Block> m_blocks;
        public void Execute(int index)
        {
            var temp = m_blocks[index];
            temp.Free();
            m_blocks[index] = temp;
        }
    }

    [Test]
    public void AllocatesAndFreesFromBurst()
    {
        AllocatorManager.Initialize();

        const int kLength = 100;
        var blocks = new NativeArray<AllocatorManager.Block>(kLength, Allocator.Persistent);
        var allocateJob = new AllocateJob();
        allocateJob.m_blocks = blocks;
        allocateJob.Schedule(kLength, 1).Complete();

        for (int i = 0; i < kLength; ++i)
        {
            var block = allocateJob.m_blocks[i];
            if(i != 0)
                Assert.AreNotEqual(IntPtr.Zero, block.Range.Pointer);
            Assert.AreEqual(i, block.Range.Items);
            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block.BytesPerItem);
            Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block.Alignment);
            Assert.AreEqual(AllocatorManager.Persistent.Value, block.Range.Allocator.Value);
        }

        var freeJob = new FreeJob();
        freeJob.m_blocks = blocks;
        freeJob.Schedule(kLength, 1).Complete();

        for (int i = 0; i < kLength; ++i)
        {
            var block = allocateJob.m_blocks[i];
            Assert.AreEqual(IntPtr.Zero, block.Range.Pointer);
            Assert.AreEqual(0, block.Range.Items);
            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block.BytesPerItem);
            Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block.Alignment);
            Assert.AreEqual(AllocatorManager.Persistent.Value, block.Range.Allocator.Value);
        }
        blocks.Dispose();
        AllocatorManager.Shutdown();
    }

    // This allocator wraps UnsafeUtility.Malloc, but also initializes memory to some constant value after allocating.
    [BurstCompile(CompileSynchronously = true)]
    struct ClearToValueAllocator : AllocatorManager.IAllocator
    {
        public AllocatorManager.AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

        public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

        public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

        internal AllocatorManager.AllocatorHandle m_handle;
        internal AllocatorManager.AllocatorHandle m_parent;

        public byte m_clearValue;

        public void Initialize<T>(byte ClearValue, ref T parent) where T : unmanaged, AllocatorManager.IAllocator
        {
            m_parent = parent.Handle;
            m_clearValue = ClearValue;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AllocatorManager.Register(ref this);
            parent.Handle.AddChildAllocator(m_handle);
#endif
        }

        public unsafe int Try(ref AllocatorManager.Block block)
        {
            var temp = block.Range.Allocator;
            block.Range.Allocator = m_parent;
            var error = AllocatorManager.Try(ref block);
            block.Range.Allocator = temp;
            if (error != 0)
                return error;
            if (block.Range.Pointer != IntPtr.Zero) // if we allocated or reallocated...
                UnsafeUtility.MemSet((void*)block.Range.Pointer, m_clearValue, block.Bytes); // clear to a value.
            return 0;
        }

        [BurstCompile(CompileSynchronously = true)]
		[MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        public static unsafe int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            return ((ClearToValueAllocator*)state)->Try(ref block);
        }

        public AllocatorManager.TryFunction Function => Try;
        public void Dispose()
        {
            m_handle.Dispose();
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    internal struct CountingAllocator : AllocatorManager.IAllocator
    {
        public AllocatorManager.AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

        public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

        public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }
        public AllocatorManager.AllocatorHandle m_handle;
        public int AllocationCount;
        public bool WasUsed => AllocationCount > 0;
        public void Initialize()
        {
            AllocationCount = 0;
            AllocatorManager.Register(ref this);
#if ENABLE_UNITY_ALLOCATIONS_CHECKS
            AllocatorManager.Persistent.Handle.AddChildAllocator(m_handle);
#endif
        }

        public int Try(ref AllocatorManager.Block block)
        {
            var temp = block.Range.Allocator;
            block.Range.Allocator = AllocatorManager.Persistent;
            var error = AllocatorManager.Try(ref block);
            block.Range.Allocator = temp;
            if (error != 0)
                return error;
            if (block.Range.Pointer != IntPtr.Zero) // if we allocated or reallocated...
                ++AllocationCount;
            return 0;
        }

        [BurstCompile(CompileSynchronously = true)]
        [MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        public static unsafe int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            return ((CountingAllocator*)state)->Try(ref block);
        }

        public AllocatorManager.TryFunction Function => Try;
        public void Dispose()
        {
            m_handle.Dispose();
        }
    }

    [Test]
    public void UserDefinedAllocatorWorks()
    {
        AllocatorManager.Initialize();
        var parent = AllocatorManager.Persistent;
        ClearToValueAllocator allocator = default;
        allocator.Initialize(0, ref parent);
        for (byte ClearValue = 0; ClearValue < 0xF; ++ClearValue)
        {
            allocator.m_clearValue = ClearValue;
            const int kLength = 100;
            for (int i = 1; i < kLength; ++i)
            {
                var block = allocator.AllocateBlock(default(int), i);
                Assert.AreNotEqual(IntPtr.Zero, block.Range.Pointer);
                Assert.AreEqual(i, block.Range.Items);
                Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block.BytesPerItem);
                Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block.Alignment);
                allocator.FreeBlock(ref block);
            }
        }
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }

    // this is testing for the case where we want+ to install a stack allocator that itself allocates from a big hunk
    // of memory provided by the default Persistent allocator, and then make allocations on the stack allocator.
    [Test]
    public void StackAllocatorWorks()
    {
        AllocatorManager.Initialize();
        var origin = AllocatorManager.Persistent;
        var backingStorage = origin.AllocateBlock(default(byte), 100000); // allocate a block of bytes from Malloc.Persistent
        AllocatorManager.StackAllocator allocator = default;
        allocator.Initialize(backingStorage);
        const int kLength = 100;
        for (int i = 1; i < kLength; ++i)
        {
            var block = allocator.AllocateBlock(default(int), i);
            Assert.AreNotEqual(IntPtr.Zero, block.Range.Pointer);
            Assert.AreEqual(i, block.Range.Items);
            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block.BytesPerItem);
            Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block.Alignment);
            allocator.FreeBlock(ref block);
        }
        allocator.Dispose();
        backingStorage.Dispose();
        AllocatorManager.Shutdown();
    }

    [Test]
    public void CustomAllocatorNativeListWorksWithoutHandles()
    {
        AllocatorManager.Initialize();
        var allocator = AllocatorManager.Persistent;
        var list = NativeList<byte>.New(100, ref allocator);
        list.Dispose(ref allocator);
        AllocatorManager.Shutdown();
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void CustomAllocatorNativeListThrowsWhenAllocatorIsWrong()
    {
        AllocatorManager.Initialize();
        var allocator0 = AllocatorManager.Persistent;
        var allocator1 = AllocatorManager.TempJob;
        var list = NativeList<byte>.New(100, ref allocator0);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            list.Dispose(ref allocator1);
        });
        list.Dispose(ref allocator0);
        AllocatorManager.Shutdown();
    }
#endif

    // this is testing for the case where we want to install a custom allocator that clears memory to a constant
    // byte value, and then have an UnsafeList use that custom allocator.
    [Test]
    public void CustomAllocatorUnsafeListWorks()
    {
        AllocatorManager.Initialize();
        var parent = AllocatorManager.Persistent;
        ClearToValueAllocator allocator = default;
        allocator.Initialize(0xFE, ref parent);
        allocator.Register();
        for (byte ClearValue = 0; ClearValue < 0xF; ++ClearValue)
        {
            allocator.m_clearValue = ClearValue;
            var unsafelist = new UnsafeList<byte>(1, allocator.Handle);
            const int kLength = 100;
            unsafelist.Resize(kLength);
            for (int i = 0; i < kLength; ++i)
                Assert.AreEqual(ClearValue, unsafelist[i]);
            unsafelist.Dispose();
        }
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }

    [Test]
    public void SlabAllocatorWorks()
    {
        var SlabSizeInBytes = 256;
        var SlabSizeInInts = SlabSizeInBytes / sizeof(int);
        var Slabs = 256;
        AllocatorManager.Initialize();
        var origin = AllocatorManager.Persistent;
        var backingStorage = origin.AllocateBlock(default(byte), Slabs * SlabSizeInBytes); // allocate a block of bytes from Malloc.Persistent
        AllocatorManager.SlabAllocator allocator = default;
        allocator.Initialize(backingStorage, SlabSizeInBytes, Slabs * SlabSizeInBytes);

        var block0 = allocator.AllocateBlock(default(int), SlabSizeInInts);
        Assert.AreNotEqual(IntPtr.Zero, block0.Range.Pointer);
        Assert.AreEqual(SlabSizeInInts, block0.Range.Items);
        Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block0.BytesPerItem);
        Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block0.Alignment);
        Assert.AreEqual(1, allocator.Occupied[0]);

        var block1 = allocator.AllocateBlock(default(int), SlabSizeInInts - 1);
        Assert.AreNotEqual(IntPtr.Zero, block1.Range.Pointer);
        Assert.AreEqual(SlabSizeInInts - 1, block1.Range.Items);
        Assert.AreEqual(UnsafeUtility.SizeOf<int>(), block1.BytesPerItem);
        Assert.AreEqual(UnsafeUtility.AlignOf<int>(), block1.Alignment);
        Assert.AreEqual(3, allocator.Occupied[0]);

        allocator.FreeBlock(ref block0);
        Assert.AreEqual(2, allocator.Occupied[0]);
        allocator.FreeBlock(ref block1);
        Assert.AreEqual(0, allocator.Occupied[0]);

        Assert.Throws<ArgumentException>(() =>
        {
            allocator.AllocateBlock(default(int), 65);
        });

        allocator.Dispose();
        backingStorage.Dispose();
        AllocatorManager.Shutdown();
    }

    [Test]
    public unsafe void CollectionHelper_IsAligned()
    {
        Assert.Throws<ArgumentException>(() => CollectionHelper.IsAligned((void*)0x0, 0)); // value is 0
        Assert.Throws<ArgumentException>(() => CollectionHelper.IsAligned((void*)0x1000, 0)); // alignment is 0
        Assert.Throws<ArgumentException>(() => CollectionHelper.IsAligned((void*)0x1000, 3)); // alignment is not pow2

        for (var i = 0; i < 31; ++i)
        {
            Assert.IsTrue(CollectionHelper.IsAligned((void*)0x80000000, 1<<i));
        }
    }
}
