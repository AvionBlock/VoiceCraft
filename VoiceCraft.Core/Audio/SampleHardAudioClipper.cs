using System;
using System.Numerics;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio
{
    public class SampleHardAudioClipper : IAudioClipper
    {
        public int Read(Span<float> data)
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
                data[simdCount] = Math.Clamp(data[simdCount], -1.0f, 1.0f);
            }

            return data.Length;
        }
    }
}