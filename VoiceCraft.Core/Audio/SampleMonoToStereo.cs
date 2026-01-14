using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleMonoToStereo
    {
        public static int Read(Span<float> data, int count, Span<float> dstBuffer)
        {
            var destOffset = 0;
            for (var i = 0; i < count; i++)
            {
                var sampleVal = data[i];
                dstBuffer[destOffset++] = sampleVal;
                dstBuffer[destOffset++] = sampleVal;
            }

            return count * 2;
        }
    }
}