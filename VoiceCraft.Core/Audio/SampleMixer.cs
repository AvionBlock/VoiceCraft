using System;
using System.Numerics;

namespace VoiceCraft.Core.Audio
{
    public static class SampleMixer
    {
        public static int Read(Span<float> data, int count, Span<float> dstBuffer)
        {
            //Usage of SIMD accelerated operation.
            var simdCount = 0;
            var simdLength = Vector<float>.Count;

            // Ensure there's enough data for SIMD operations
            if (Vector.IsHardwareAccelerated && count >= simdLength)
            {
                // Process SIMD chunks
                for (; simdCount <= count - simdLength; simdCount += simdLength)
                {
                    var vectorS = new Vector<float>(data.Slice(simdCount, Vector<float>.Count));
                    var vectorD = new Vector<float>(dstBuffer.Slice(simdCount, Vector<float>.Count));
                    vectorD += vectorS;
                    vectorD.CopyTo(dstBuffer.Slice(simdCount, simdLength));
                }
            }
            
            // Scalar remainder
            for (; simdCount < count; simdCount++)
            {
                dstBuffer[simdCount] += data[simdCount];
            }

            return count;
        }
    }
}