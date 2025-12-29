using System;

namespace VoiceCraft.Core.Audio
{
    public class SineWaveGenerator
    {
        private float _currentPhase;

        // Internal state
        private float _phaseIncrement;

        public SineWaveGenerator(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public int SampleRate { get; }

        public float Frequency { get; set; } = 440f; // A4 note

        public float Amplitude { get; set; } = 1.0f;

        public float Phase { get; set; } = 0f;

        public int Read(Span<float> buffer, int count)
        {
            // Calculate the phase increment per sample based on the frequency
            _phaseIncrement = (float)(2.0 * Math.PI * Frequency / SampleRate);

            for (var i = 0; i < count; i++) buffer[i] = GenerateSample();
            return count;
        }

        public int Read(float[] buffer, int count)
        {
            return Read(buffer.AsSpan(), count);
        }

        private float GenerateSample()
        {
            var sampleValue = MathF.Sin(_currentPhase + Phase);

            _currentPhase += _phaseIncrement;
            if (_currentPhase >= 2.0 * Math.PI) _currentPhase -= (float)(2.0 * Math.PI);

            return sampleValue * Amplitude;
        }
    }
}