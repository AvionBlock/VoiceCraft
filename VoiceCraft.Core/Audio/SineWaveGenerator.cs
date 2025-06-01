using System;
using System.Runtime.InteropServices;

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

        //This works somehow...
        public int Read(byte[] buffer, int offset, int count)
        {
            // Calculate the phase increment per sample based on the frequency
            _phaseIncrement = (float)(2.0 * Math.PI * Frequency / SampleRate);

            var shortBuffer = MemoryMarshal.Cast<byte, short>(buffer);
            for (var i = 0; i < shortBuffer.Length; i++) shortBuffer[i] = FloatToShort(GenerateSample());

            return count;
        }

        private short FloatToShort(float value)
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