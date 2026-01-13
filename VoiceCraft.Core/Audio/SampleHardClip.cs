using System;
using System.Numerics;

namespace VoiceCraft.Core.Audio
{
    public static class SampleHardClip
    {
        public static int Read(Span<float> data, int count)
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
                    vectorS = Vector.Min(Vector.Max(vectorS, -Vector<float>.One), Vector<float>.One);
                    vectorS.CopyTo(data.Slice(simdCount, simdLength));
                }
            }
            
            // Scalar remainder
            for (; simdCount < count; simdCount++)
            {
                data[simdCount] += data[simdCount];
            }

            return count;
        }
    }
}