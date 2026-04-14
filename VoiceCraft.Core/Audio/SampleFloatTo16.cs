using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleFloatTo16
    {
        public static int Read(Span<float> data, Span<short> dstBuffer)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var clamped = Math.Clamp(data[i], -1f, 1f);
                dstBuffer[i] = clamped <= -1f
                    ? short.MinValue
                    : (short)(clamped * short.MaxValue);
            }
            return data.Length;
        }
    }
}
