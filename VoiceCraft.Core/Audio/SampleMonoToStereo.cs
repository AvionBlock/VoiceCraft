using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleMonoToStereo
    {
        public static int Read(Span<float> data, Span<float> dstBuffer)
        {
            var destOffset = 0;
            foreach (var sampleVal in data)
            {
                dstBuffer[destOffset++] = sampleVal;
                dstBuffer[destOffset++] = sampleVal;
            }

            return data.Length * 2;
        }
    }
}