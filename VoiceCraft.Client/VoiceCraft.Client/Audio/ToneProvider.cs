using System;
using System.Threading;
using SoundFlow.Abstracts;
using SoundFlow.Structs;

namespace VoiceCraft.Client.Audio;

public sealed class ToneProvider(AudioEngine engine, AudioFormat format) : SoundComponent(engine, format)
{
    private readonly Lock _lock = new();
    private readonly int _fadeSamples = Math.Max(1, format.SampleRate / 100);
    private int _totalFrames = Math.Max(1, 0 * format.SampleRate);
    private float _amplitude = 0.18f;
    private float _frequency;
    private int _frameIndex;

    public void Play(TimeSpan duration, float frequency, float amplitude = 0.18f)
    {
        lock (_lock)
        {
            _frequency = frequency;
            _amplitude = amplitude;
            _totalFrames = Math.Max(1, (int)(duration.TotalSeconds * Format.SampleRate));
            _frameIndex = 0;
        }
    }

    protected override void GenerateAudio(Span<float> buffer, int channels)
    {
        buffer.Clear();

        lock (_lock)
        {
            for (var sampleIndex = 0; sampleIndex < buffer.Length; sampleIndex += channels)
            {
                if (_frameIndex >= _totalFrames)
                    break;

                var fadeMultiplier = GetFadeMultiplier(_frameIndex);
                var sample = MathF.Sin((2f * MathF.PI * _frequency * _frameIndex) / Format.SampleRate) * _amplitude *
                             fadeMultiplier;

                for (var channel = 0; channel < channels && sampleIndex + channel < buffer.Length; channel++)
                    buffer[sampleIndex + channel] = sample;

                _frameIndex++;
            }
        }
    }

    private float GetFadeMultiplier(int frame)
    {
        if (frame < _fadeSamples)
            return frame / (float)_fadeSamples;

        var remaining = _totalFrames - frame;
        return remaining < _fadeSamples ? Math.Max(0f, remaining / (float)_fadeSamples) : 1f;
    }
}