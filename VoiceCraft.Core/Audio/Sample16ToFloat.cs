using System;

namespace VoiceCraft.Core.Audio
{
    public static class Sample16ToFloat
    {
        public static int Read(Span<short> buffer, Span<float> dstBuffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                dstBuffer[i] = Math.Clamp(buffer[i] / (short.MaxValue + 1f), -1f, 1f);
            return buffer.Length;
        }
    }
}