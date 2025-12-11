using System;

namespace VoiceCraft.Core.Audio
{
    public class SineWaveGenerator16
    {
        private float _currentPhase;

        // Internal state
        private float _phaseIncrement;

        public SineWaveGenerator16(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public int SampleRate { get; }

        public float Frequency { get; set; } = 440f; // A4 note

        public float Amplitude { get; set; } = 1.0f;

        public float Phase { get; set; } = 0f;

        public int Read(Span<short> buffer, int count)
        {
            // Calculate the phase increment per sample based on the frequency
            _phaseIncrement = (float)(2.0 * Math.PI * Frequency / SampleRate);

            for (var i = 0; i < count; i++) buffer[i] = FloatToShort(GenerateSample());
            return count;
        }

        public int Read(short[] buffer, int count)
        {
            return Read(buffer.AsSpan(), count);
        }

        private static short FloatToShort(float value)
        {
            return (short)(value * short.MaxValue);
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