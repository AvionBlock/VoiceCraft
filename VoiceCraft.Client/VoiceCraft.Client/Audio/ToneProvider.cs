using System;
using SoundFlow.Abstracts;
using SoundFlow.Structs;

namespace VoiceCraft.Client.Audio;

public sealed class ToneProvider : SoundComponent
{
    private readonly float _amplitude;
    private readonly int _fadeSamples;
    private readonly float _frequency;
    private readonly int _totalFrames;
    private int _frameIndex;

    public ToneProvider(AudioEngine engine, AudioFormat format, float frequency, TimeSpan duration, float amplitude = 0.18f)
        : base(engine, format)
    {
        _frequency = frequency;
        _amplitude = amplitude;
        _totalFrames = Math.Max(1, (int)(duration.TotalSeconds * format.SampleRate));
        _fadeSamples = Math.Max(1, format.SampleRate / 100);
    }

    public bool IsFinished => _frameIndex >= _totalFrames;

    protected override void GenerateAudio(Span<float> buffer, int channels)
    {
        buffer.Clear();

        for (var sampleIndex = 0; sampleIndex < buffer.Length; sampleIndex += channels)
        {
            if (_frameIndex >= _totalFrames)
                break;

            var fadeMultiplier = GetFadeMultiplier(_frameIndex);
            var sample = MathF.Sin((2f * MathF.PI * _frequency * _frameIndex) / Format.SampleRate) * _amplitude * fadeMultiplier;

            for (var channel = 0; channel < channels && sampleIndex + channel < buffer.Length; channel++)
                buffer[sampleIndex + channel] = sample;

            _frameIndex++;
        }
    }

    private float GetFadeMultiplier(int frame)
    {
        if (frame < _fadeSamples)
            return frame / (float)_fadeSamples;

        var remaining = _totalFrames - frame;
        if (remaining < _fadeSamples)
            return Math.Max(0f, remaining / (float)_fadeSamples);

        return 1f;
    }
}
