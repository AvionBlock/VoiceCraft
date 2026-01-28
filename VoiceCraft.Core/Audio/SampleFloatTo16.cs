using System;

namespace VoiceCraft.Core.Audio
{
    public static class SampleFloatTo16
    {
        public static int Read(Span<float> data, Span<short> dstBuffer)
        {
            for (var i = 0; i < data.Length; i++)
                dstBuffer[i] = Math.Clamp((short)(data[i] * short.MaxValue), short.MinValue, short.MaxValue);
            return data.Length;
        }
    }
}