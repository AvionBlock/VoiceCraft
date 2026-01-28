using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio
{
    public class SampleTanhSoftAudioClipper : IAudioClipper
    {
        public int Read(Span<float> data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (float)Math.Tanh(data[i]);
            }

            return data.Length;
        }
    }
}