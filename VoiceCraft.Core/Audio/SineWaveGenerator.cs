using System;
using System.Runtime.InteropServices;

namespace VoiceCraft.Core.Audio
{
    public class SineWaveGenerator
    {
        public int SampleRate { get; }

        public float Frequency { get; set; } = 440f; // A4 note

        public float Amplitude { get; set; } = 1.0f;

        public float Phase { get; set; } = 0f;

        // Internal state
        private float _phaseIncrement;
        private float _currentPhase;

        public SineWaveGenerator(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // Calculate the phase increment per sample based on the frequency
            _phaseIncrement = (float)(2.0 * Math.PI * Frequency / SampleRate);

            var floatBuffer = MemoryMarshal.Cast<byte, float>(buffer);
            for (var i = 0; i < count / sizeof(float); i++)
            {
                floatBuffer[i] = GenerateSample();
            }

            return count;
        }

        private float GenerateSample()
        {
            var sampleValue = MathF.Sin(_currentPhase + Phase);

            _currentPhase += _phaseIncrement;
            if (_currentPhase >= 2.0 * Math.PI)
            {
                _currentPhase -= (float)(2.0 * Math.PI);
            }

            return sampleValue * Amplitude;
        }
    }
}