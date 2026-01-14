using System;

namespace VoiceCraft.Core.Audio
{
    public static class Sample16ToFloat
    {
        public static int Read(Span<short> data, int count, Span<float> dstBuffer)
        {
            for (var i = 0; i < count; i++)
                dstBuffer[i] = Math.Clamp(data[i] / (short.MaxValue + 1f), -1f, 1f);
            return count;
        }
    }
}