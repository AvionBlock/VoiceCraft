using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleLoudness16
    {
        public static float Read(Span<short> data, int count)
        {
            float max = 0;
            // interpret as 16-bit audio
            for (var i = 0; i < count; i++)
            {
                var sample = data[i];
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            return max;
        }
    }
}