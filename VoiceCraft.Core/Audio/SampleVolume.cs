using System;
using System.Numerics;

namespace VoiceCraft.Core.Audio
{
    public static class SampleVolume
    {
        public static int Read(Span<float> data, float volume)
        {
            if (Math.Abs(volume - 1.0f) < Constants.FloatingPointTolerance)
                return data.Length; // Skip if volume is essentially 1.0
            
            volume = Math.Clamp(volume, 0.0f, 2.0f); //Clamp the values.
            
            //Usage of SIMD accelerated operation.
            var simdCount = 0;
            var simdLength = Vector<float>.Count;

            // Ensure there's enough data for SIMD operations
            if (Vector.IsHardwareAccelerated && data.Length >= simdLength)
            {
                var volumeVector = new Vector<float>(volume);
                
                // Process SIMD chunks
                for (; simdCount <= data.Length - simdLength; simdCount += simdLength)
                {
                    var vectorS = new Vector<float>(data.Slice(simdCount, Vector<float>.Count));
                    vectorS *= volumeVector;
                    vectorS.CopyTo(data.Slice(simdCount, simdLength));
                }
            }
            
            // Process remaining samples (scalar fallback)
            for (; simdCount < data.Length; simdCount++)
            {
                data[simdCount] *= volume;
            }

            return data.Length;
        }
    }
}