using NAudio.Wave;
using System;

namespace VoiceCraft.Core.Audio
{
    public class EchoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] delayBuffer;
        private int delayPos;
        private readonly int delayLength;

        public float EchoFactor { get; set; }
        public float DecayFactor { get; set; }

        public WaveFormat WaveFormat => source.WaveFormat;

        public EchoSampleProvider(ISampleProvider source, int echoDelayMs = 50)
        {
            this.source = source;
            // Calculate delay in samples
            int channels = source.WaveFormat.Channels;
            delayLength = (int)((echoDelayMs / 1000.0) * source.WaveFormat.SampleRate * channels);
            
            // Ensure alignment with channels to prevent channel bleeding
            if (channels > 0 && delayLength % channels != 0)
            {
                delayLength -= (delayLength % channels);
            }
            if (delayLength < channels) delayLength = channels; // Minimum 1 frame

            delayBuffer = new float[delayLength];
            delayPos = 0;
            EchoFactor = 0.0f;
            DecayFactor = 0.8f;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            if (EchoFactor == 0) return read;

            // Local copies for thread safety (tearing prevention)
            float ef = EchoFactor;
            float df = DecayFactor;

            for (int i = 0; i < read; i++)
            {
                float input = buffer[offset + i];
                float delaySample = delayBuffer[delayPos];

                float output = input + (delaySample * ef);

                // Hard clamp for safety
                if (output > 1.0f) output = 1.0f;
                else if (output < -1.0f) output = -1.0f;

                buffer[offset + i] = output;

                // Simple feedback loop
                float feedback = output * df; // Feeding back the wet signal
                if (feedback > 1.0f) feedback = 1.0f;
                else if (feedback < -1.0f) feedback = -1.0f;

                delayBuffer[delayPos] = feedback;

                delayPos++;
                if (delayPos >= delayLength)
                {
                    delayPos = 0;
                }
            }

            return read;
        }
    }
}
//Credits https://www.youtube.com/watch?v=ZiIJRvNx2N0&t