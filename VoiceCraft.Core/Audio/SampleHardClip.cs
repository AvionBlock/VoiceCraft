using System;
using System.Numerics;

namespace VoiceCraft.Core.Audio
{
    public static class SampleHardClip
    {
        public static int Read(Span<float> data)
        {
            //Usage of SIMD accelerated operation.
            var simdCount = 0;
            var simdLength = Vector<float>.Count;

            // Ensure there's enough data for SIMD operations
            if (Vector.IsHardwareAccelerated && data.Length >= simdLength)
            {
                // Process SIMD chunks
                for (; simdCount <= data.Length - simdLength; simdCount += simdLength)
                {
                    var vectorS = new Vector<float>(data.Slice(simdCount, Vector<float>.Count));
                    vectorS = Vector.Min(Vector.Max(vectorS, -Vector<float>.One), Vector<float>.One);
                    vectorS.CopyTo(data.Slice(simdCount, simdLength));
                }
            }
            
            // Scalar remainder
            for (; simdCount < data.Length; simdCount++)
            {
                data[simdCount] += data[simdCount];
            }

            return data.Length;
        }
    }
}