using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleLoudness
    {
        public static float Read(Span<float> data)
        {
            float max = 0;
            // interpret as 16-bit audio
            foreach (var sample in data)
            {
                var absoluteSample = Math.Abs(sample);
                max = Math.Max(max, absoluteSample);
            }

            return max;
        }
    }
}