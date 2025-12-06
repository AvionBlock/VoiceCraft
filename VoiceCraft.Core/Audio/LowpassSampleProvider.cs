using NAudio.Dsp;
using NAudio.Wave;

namespace VoiceCraft.Core.Audio
{
    public class LowpassSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly BiQuadFilter[] filters;
        private readonly int channels;

        public bool Enabled { get; set; } = true;

        public LowpassSampleProvider(ISampleProvider source, int cutOffFreq, int bandWidth)
        {
            this.source = source;
            this.channels = source.WaveFormat.Channels;
            this.filters = new BiQuadFilter[channels];
            
            for (int i = 0; i < channels; i++)
            {
                filters[i] = BiQuadFilter.LowPassFilter(source.WaveFormat.SampleRate, cutOffFreq, bandWidth);
            }
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            if (Enabled)
            {
                for (int i = 0; i < samplesRead; i++)
                {
                    // Map sample index to channel index for interleaved audio
                    // 0 -> Ch0, 1 -> Ch1, 2 -> Ch0, 3 -> Ch1 ...
                    int ch = i % channels;
                    buffer[offset + i] = filters[ch].Transform(buffer[offset + i]);
                }
            }

            return samplesRead;
        }
    }
}
