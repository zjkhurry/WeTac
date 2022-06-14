using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    /// <summary>
    /// A set of untyped, append-only buffers. Allows for concurrent reading and concurrent writing without synchronization.
    /// </summary>
    /// <remarks>
    /// As long as each individual buffer is written in one thread and read in one thread, multiple
    /// threads can read and write the stream concurrently, *e.g.*
    /// while thread *A* reads from buffer *X* of a stream, thread *B* can read from
    /// buffer *Y* of the same stream.
    ///
    /// Each buffer is stored as a chain of blocks. When a write exceeds a buffer's current capacity, another block
    /// is allocated and added to the end of the chain. Effectively, expanding the buffer never requires copying the existing
    /// data (unlike with <see cref="NativeList{T}"/>, for example).
    ///
    /// **All writing to a stream should be completed before the stream is first read. Do not write to a stream after the first read.**
    /// Violating these rules won't *necessarily* cause any problems, but they are the intended usage pattern.
    ///
    /// Writing is done with <see cref="NativeStream.Writer"/>, and reading is done with <see cref="NativeStream.Reader"/>.
    /// An individual reader or writer cannot be used concurrently across threads: each thread must use its own.
    ///
    /// The data written to an individual buffer can be heterogeneous in type, and the data written
    /// to different buffers of a stream can be entirely different in type, number, and order. Just make sure
    /// that the code reading from a particular buffer knows what to expect to read from it.
    /// </remarks>
    [NativeContainer]
    [BurstCompatible]
    public unsafe struct NativeStream : IDisposable
    {
        UnsafeStream m_Stream;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        /// Initializes and returns an instance of NativeStream.
        /// </summary>
        /// <param name="bufferCount">The number of buffers to give the stream. You usually want
        /// one buffer for each thread that will read or write the stream.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeStream(int bufferCount, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out this, allocator);
            m_Stream.AllocateForEach(bufferCount);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used on the main thread after completing the returned job or used in other jobs that depend upon the returned job.
        ///
        /// Using a job to allocate the buffers can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <typeparam name="T">Ignored.</typeparam>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">A list whose length determines the number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public static JobHandle ScheduleConstruct<T>(out NativeStream stream, NativeList<T> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJobList { List = (UntypedUnsafeList*)bufferCount.GetUnsafeList(), Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used...
        /// - after completing the returned job
        /// - or in other jobs that depend upon the returned job.
        ///
        /// Allocating the buffers in a job can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">An array whose value at index 0 determines the number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public static JobHandle ScheduleConstruct(out NativeStream stream, NativeArray<int> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJob { Length = bufferCount, Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Returns true if this stream is empty.
        /// </summary>
        /// <returns>True if this stream is empty.</returns>
        public bool IsEmpty()
        {
            CheckReadAccess();
            return m_Stream.IsEmpty();
        }

        /// <summary>
        /// Whether this stream has been allocated (and not yet deallocated).
        /// </summary>
        /// <remarks>Does not necessarily reflect whether the buffers of the stream have themselves been allocated.</remarks>
        /// <value>True if this stream has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Stream.IsCreated;

        /// <summary>
        /// The number of buffers in this stream.
        /// </summary>
        /// <value>The number of buffers in this stream.</value>
        public int ForEachCount
        {
            get
            {
                CheckReadAccess();
                return m_Stream.ForEachCount;
            }
        }

        /// <summary>
        /// Returns a reader of this stream.
        /// </summary>
        /// <returns>A reader of this stream.</returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        /// <summary>
        /// Returns a writer of this stream.
        /// </summary>
        /// <returns>A writer of this stream.</returns>
        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        /// <summary>
        /// Returns the total number of items in the buffers of this stream.
        /// </summary>
        /// <remarks>Each <see cref="Writer.Write{T}"/> and <see cref="Writer.Allocate"/> call increments this number.</remarks>
        /// <returns>The total number of items in the buffers of this stream.</returns>
        public int Count()
        {
            CheckReadAccess();
            return m_Stream.Count();
        }

        /// <summary>
        /// Returns a new NativeArray copy of this stream's data.
        /// </summary>
        /// <remarks>The length of the array will equal the count of this stream.
        ///
        /// Each buffer of this stream is copied to the array, one after the other.
        /// </remarks>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A new NativeArray copy of this stream's data.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public NativeArray<T> ToNativeArray<T>(AllocatorManager.AllocatorHandle allocator) where T : struct
        {
            CheckReadAccess();
            return m_Stream.ToNativeArray<T>(allocator);
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            m_Stream.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will release all resources (memory and safety handles) of this stream.
        /// </summary>
        /// <param name="inputDeps">A job handle which the newly scheduled job will depend upon.</param>
        /// <returns>The handle of a new job that will release all resources (memory and safety handles) of this stream.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = m_Stream.Dispose(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            return jobHandle;
        }

        [BurstCompile]
        struct ConstructJobList : IJob
        {
            public NativeStream Container;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public UntypedUnsafeList* List;

            public void Execute()
            {
                Container.AllocateForEach(List->Length);
            }
        }

        [BurstCompile]
        struct ConstructJob : IJob
        {
            public NativeStream Container;

            [ReadOnly]
            public NativeArray<int> Length;

            public void Execute()
            {
                Container.AllocateForEach(Length[0]);
            }
        }

        static void AllocateBlock(out NativeStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            CollectionHelper.CheckAllocator(allocator);

            UnsafeStream.AllocateBlock(out stream.m_Stream, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(allocator.IsCustomAllocator)
            {
                stream.m_Safety = AtomicSafetyHandle.Create();
                stream.m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out stream.m_Safety, out stream.m_DisposeSentinel, 0, allocator.ToAllocator);
            }
#endif
        }

        void AllocateForEach(int forEachCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckForEachCountGreaterThanZero(forEachCount);
            Assert.IsTrue(m_Stream.m_Block->Ranges == null);
            Assert.AreEqual(0, m_Stream.m_Block->RangeCount);
            Assert.AreNotEqual(0, m_Stream.m_Block->BlockCount);
#endif

            m_Stream.AllocateForEach(forEachCount);
        }

        /// <summary>
        /// Writes data into a buffer of a <see cref="NativeStream"/>.
        /// </summary>
        /// <remarks>An individual writer can only be used for one buffer of one stream.
        /// Do not create more than one writer for an individual buffer.</remarks>
        [NativeContainer]
        [NativeContainerSupportsMinMaxWriteRestriction]
        [BurstCompatible]
        public unsafe struct Writer
        {
            UnsafeStream.Writer m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#pragma warning disable CS0414 // warning CS0414: The field 'NativeStream.Writer.m_Length' is assigned but its value is never used
            int m_Length;
#pragma warning restore CS0414
            int m_MinIndex;
            int m_MaxIndex;

            [NativeDisableUnsafePtrRestriction]
            void* m_PassByRefCheck;
#endif

            internal Writer(ref NativeStream stream)
            {
                m_Writer = stream.m_Stream.AsWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = stream.m_Safety;
                m_Length = int.MaxValue;
                m_MinIndex = int.MinValue;
                m_MaxIndex = int.MinValue;
                m_PassByRefCheck = null;
#endif
            }

            /// <summary>
            /// The number of buffers in the stream of this writer.
            /// </summary>
            /// <value>The number of buffers in the stream of this writer.</value>
            public int ForEachCount
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                    return m_Writer.ForEachCount;
                }
            }

            /// <summary>
            /// For internal use only.
            /// </summary>
            /// <param name="foreEachIndex"></param>
            public void PatchMinMaxRange(int foreEachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_MinIndex = foreEachIndex;
                m_MaxIndex = foreEachIndex;
#endif
            }

            /// <summary>
            /// Readies this writer to write to a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this writer. For an individual writer, call this method only once.
            ///
            /// When done using this writer, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to write.</param>
            public void BeginForEachIndex(int foreachIndex)
            {
                //@TODO: Check that no one writes to the same for each index multiple times...
                CheckBeginForEachIndex(foreachIndex);
                m_Writer.BeginForEachIndex(foreachIndex);
            }

            /// <summary>
            /// Readies the buffer written by this writer for reading.
            /// </summary>
            /// <remarks>Must be called before reading the buffer written by this writer.</remarks>
            public void EndForEachIndex()
            {
                CheckEndForEachIndex();
                m_Writer.EndForEachIndex();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Writer.m_ForeachIndex = int.MinValue;
#endif
            }

            /// <summary>
            /// Write a value to a buffer.
            /// </summary>
            /// <remarks>The value is written to the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <typeparam name="T">The type of value to write.</typeparam>
            /// <param name="value">The value to write.</param>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public void Write<T>(T value) where T : struct
            {
                ref T dst = ref Allocate<T>();
                dst = value;
            }

            /// <summary>
            /// Allocate space in a buffer.
            /// </summary>
            /// <remarks>The space is allocated in the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <typeparam name="T">The type of value to allocate space for.</typeparam>
            /// <returns>A reference to the allocation.</returns>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public ref T Allocate<T>() where T : struct
            {
                CollectionHelper.CheckIsUnmanaged<T>();
                int size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(Allocate(size));
            }

            /// <summary>
            /// Allocate space in a buffer.
            /// </summary>
            /// <remarks>The space is allocated in the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <param name="size">The number of bytes to allocate.</param>
            /// <returns>The allocation.</returns>
            public byte* Allocate(int size)
            {
                CheckAllocateSize(size);
                return m_Writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckBeginForEachIndex(int foreachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                if (m_PassByRefCheck == null)
                {
                    m_PassByRefCheck = UnsafeUtility.AddressOf(ref this);
                }

                if (foreachIndex < m_MinIndex || foreachIndex > m_MaxIndex)
                {
                    // When the code is not running through the job system no ParallelForRange patching will occur
                    // We can't grab m_BlockStream->RangeCount on creation of the writer because the RangeCount can be initialized
                    // in a job after creation of the writer
                    if (m_MinIndex == int.MinValue && m_MaxIndex == int.MinValue)
                    {
                        m_MinIndex = 0;
                        m_MaxIndex = m_Writer.m_BlockStream->RangeCount - 1;
                    }

                    if (foreachIndex < m_MinIndex || foreachIndex > m_MaxIndex)
                    {
                        throw new ArgumentException($"Index {foreachIndex} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in NativeStream.");
                    }
                }

                if (m_Writer.m_ForeachIndex != int.MinValue)
                {
                    throw new ArgumentException($"BeginForEachIndex must always be balanced by a EndForEachIndex call");
                }

                if (0 != m_Writer.m_BlockStream->Ranges[foreachIndex].ElementCount)
                {
                    throw new ArgumentException($"BeginForEachIndex can only be called once for the same index ({foreachIndex}).");
                }

                Assert.IsTrue(foreachIndex >= 0 && foreachIndex < m_Writer.m_BlockStream->RangeCount);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckEndForEachIndex()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                if (m_Writer.m_ForeachIndex == int.MinValue)
                {
                    throw new System.ArgumentException("EndForEachIndex must always be called balanced by a BeginForEachIndex or AppendForEachIndex call");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckAllocateSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                if (m_PassByRefCheck != UnsafeUtility.AddressOf(ref this))
                {
                    throw new ArgumentException("NativeStream.Writer must be passed by ref once it is in use");
                }

                if (m_Writer.m_ForeachIndex == int.MinValue)
                {
                    throw new ArgumentException("Allocate must be called within BeginForEachIndex / EndForEachIndex");
                }

                if (size > UnsafeStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
#endif
            }
        }

        /// <summary>
        /// Reads data from a buffer of a <see cref="NativeStream"/>.
        /// </summary>
        /// <remarks>An individual reader can only be used for one buffer of one stream.
        /// Do not create more than one reader for an individual buffer.</remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [BurstCompatible]
        public unsafe struct Reader
        {
            UnsafeStream.Reader m_Reader;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int m_RemainingBlocks;
            internal AtomicSafetyHandle m_Safety;
#endif

            internal Reader(ref NativeStream stream)
            {
                m_Reader = stream.m_Stream.AsReader();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_RemainingBlocks = 0;
                m_Safety = stream.m_Safety;
#endif
            }

            /// <summary>
            /// Readies this reader to read a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this reader. For an individual reader, call this method only once.
            ///
            /// When done using this reader, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to read.</param>
            /// <returns>The number of elements left to read from the buffer.</returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                CheckBeginForEachIndex(foreachIndex);

                var remainingItemCount = m_Reader.BeginForEachIndex(foreachIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_RemainingBlocks = m_Reader.m_BlockStream->Ranges[foreachIndex].NumberOfBlocks;
                if (m_RemainingBlocks == 0)
                {
                    m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + m_Reader.m_LastBlockSize;
                }
#endif

                return remainingItemCount;
            }

            /// <summary>
            /// Checks if all data has been read from the buffer.
            /// </summary>
            /// <remarks>If you intentionally don't want to read *all* the data in the buffer, don't call this method.
            /// Otherwise, calling this method is recommended, even though it's not strictly necessary.</remarks>
            /// <exception cref="ArgumentException">Thrown if not all the buffer's data has been read.</exception>
            public void EndForEachIndex()
            {
                m_Reader.EndForEachIndex();
                CheckEndForEachIndex();
            }

            /// <summary>
            /// The number of buffers in the stream of this reader.
            /// </summary>
            /// <value>The number of buffers in the stream of this reader.</value>
            public int ForEachCount
            {
                get
                {
                    CheckRead();
                    return m_Reader.ForEachCount;
                }
            }

            /// <summary>
            /// The number of items not yet read from the buffer.
            /// </summary>
            /// <value>The number of items not yet read from the buffer.</value>
            public int RemainingItemCount => m_Reader.RemainingItemCount;

            /// <summary>
            /// Returns a pointer to the next position to read from the buffer. Advances the reader some number of bytes.
            /// </summary>
            /// <param name="size">The number of bytes to advance the reader.</param>
            /// <returns>A pointer to the next position to read from the buffer.</returns>
            /// <exception cref="ArgumentException">Thrown if the reader would advance past the end of the buffer.</exception>
            public byte* ReadUnsafePtr(int size)
            {
                CheckReadSize(size);

                m_Reader.m_RemainingItemCount--;

                byte* ptr = m_Reader.m_CurrentPtr;
                m_Reader.m_CurrentPtr += size;

                if (m_Reader.m_CurrentPtr > m_Reader.m_CurrentBlockEnd)
                {
                    /*
                     * On netfw/mono/il2cpp, doing m_CurrentBlock->Data does not throw, because it knows that it can
                     * just do pointer + 8. On netcore, doing that throws a NullReferenceException. So, first check for
                     * out of bounds accesses, and only then update m_CurrentBlock and m_CurrentPtr.
                     */
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_RemainingBlocks--;

                    CheckNotReadingOutOfBounds(size);
#endif
                    m_Reader.m_CurrentBlock = m_Reader.m_CurrentBlock->Next;
                    m_Reader.m_CurrentPtr = m_Reader.m_CurrentBlock->Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (m_RemainingBlocks <= 0)
                    {
                        m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + m_Reader.m_LastBlockSize;
                    }
                    else
                    {
                        m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;
                    }
#else
                    m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;
#endif
                    ptr = m_Reader.m_CurrentPtr;
                    m_Reader.m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary>
            /// Reads the next value from the buffer.
            /// </summary>
            /// <remarks>Each read advances the reader to the next item in the buffer.</remarks>
            /// <typeparam name="T">The type of value to read.</typeparam>
            /// <returns>A reference to the next value from the buffer.</returns>
            /// <exception cref="ArgumentException">Thrown if the reader would advance past the end of the buffer.</exception>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public ref T Read<T>() where T : struct
            {
                int size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            /// <summary>
            /// Reads the next value from the buffer. Does not advance the reader.
            /// </summary>
            /// <typeparam name="T">The type of value to read.</typeparam>
            /// <returns>A reference to the next value from the buffer.</returns>
            /// <exception cref="ArgumentException">Thrown if the read would go past the end of the buffer.</exception>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public ref T Peek<T>() where T : struct
            {
                int size = UnsafeUtility.SizeOf<T>();
                CheckReadSize(size);

                return ref m_Reader.Peek<T>();
            }

            /// <summary>
            /// Returns the total number of items in the buffers of the stream.
            /// </summary>
            /// <returns>The total number of items in the buffers of the stream.</returns>
            public int Count()
            {
                CheckRead();
                return m_Reader.Count();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckNotReadingOutOfBounds(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_RemainingBlocks < 0)
                    throw new System.ArgumentException("Reading out of bounds");

                if (m_RemainingBlocks == 0 && size + sizeof(void*) > m_Reader.m_LastBlockSize)
                    throw new System.ArgumentException("Reading out of bounds");
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckReadSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                Assert.IsTrue(size <= UnsafeStreamBlockData.AllocationSize - (sizeof(void*)));
                if (m_Reader.m_RemainingItemCount < 1)
                {
                    throw new ArgumentException("There are no more items left to be read.");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckBeginForEachIndex(int forEachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                if ((uint)forEachIndex >= (uint)m_Reader.m_BlockStream->RangeCount)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(forEachIndex), $"foreachIndex: {forEachIndex} must be between 0 and ForEachCount: {m_Reader.m_BlockStream->RangeCount}");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckEndForEachIndex()
            {
                if (m_Reader.m_RemainingItemCount != 0)
                {
                    throw new System.ArgumentException("Not all elements (Count) have been read. If this is intentional, simply skip calling EndForEachIndex();");
                }

                if (m_Reader.m_CurrentBlockEnd != m_Reader.m_CurrentPtr)
                {
                    throw new System.ArgumentException("Not all data (Data Size) has been read. If this is intentional, simply skip calling EndForEachIndex();");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckForEachCountGreaterThanZero(int forEachCount)
        {
            if (forEachCount <= 0)
                throw new ArgumentException("foreachCount must be > 0", "foreachCount");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }
    }
}
