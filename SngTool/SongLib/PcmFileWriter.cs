using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SongLib
{
    public unsafe class PcmFileWriter : IDisposable
    {
        public const ushort BitsPerSample = 32;
        private const ushort ChannelSize = BitsPerSample / 8; // 8 bits per byte
        public readonly int SampleRate;
        public readonly ushort Channels;
        public readonly long TotalSamples;
        public readonly long TotalSize;
        public MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor accessor;
        public uint SamplesWritten = 0;

        private byte* ptrWrite;

        public static long CalculateSizeEstimate(long totalSamples)
        {
            return totalSamples * ChannelSize;
        }

        public PcmFileWriter(MemoryMappedFile file, int sampleRate, ushort channels, long totalSamples)
        {
            Channels = channels;
            TotalSamples = totalSamples;
            SampleRate = sampleRate;
            mappedFile = file;

            TotalSize = CalculateSizeEstimate(totalSamples);

            accessor = mappedFile.CreateViewAccessor();
            var mappedFileSize = accessor.SafeMemoryMappedViewHandle.ByteLength;

            if (mappedFileSize < (ulong)TotalSize)
            {
                accessor.Dispose();
                throw new ArgumentException("MemoryMappedFile too small");
            }

            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptrWrite);
        }

        public void Dispose()
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            accessor.Dispose();
        }

        /// <summary>
        /// This returns the maximum samples that we can ingest in one call 
        /// </summary>
        public int MaxChunkSamples => (Array.MaxLength / ChannelSize) & ~1;

        public bool Completed => SamplesWritten >= TotalSamples;

        long writePos = 0;

        public void IngestSamples(Span<float> audioSamples)
        {
            if (audioSamples.Length > MaxChunkSamples)
            {
                throw new ArgumentException("Too many samples, sample count should be lower than MaxChunkSamples");
            }
            int sampleCount = audioSamples.Length;
            int pcmDataSize = sampleCount * ChannelSize;

            var endPos = writePos + pcmDataSize;

            // If end pos too long clamp to max size
            if (endPos > TotalSize)
            {

                pcmDataSize = (int)(TotalSize - writePos) & ~1;
                sampleCount = pcmDataSize / ChannelSize;
                audioSamples = audioSamples.Slice(0, sampleCount);
                Console.WriteLine("Too long, clamping to max");
            }
            Span<byte> wavDataSpan = new Span<byte>(ptrWrite + writePos, pcmDataSize);

            int pos = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                // short intSample = (short)Math.Round(audioSamples[i] * short.MaxValue);
                // BinaryPrimitives.WriteInt16LittleEndian(wavDataSpan.Slice(pos, 2), intSample);
                BinaryPrimitives.WriteSingleLittleEndian(wavDataSpan.Slice(pos, 4), audioSamples[i]);
                pos += 4;
            }
            writePos += pos;
            SamplesWritten += (uint)sampleCount;
        }
    }
}