using NAudio.Wave;

namespace VoiceCraft.Core.Audio;

/// <summary>
/// A sample provider that smoothly transitions between volume levels to prevent clicks and pops.
/// </summary>
public class SmoothVolumeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private float _fadeSamplePosition;
    private int _fadeSamples;
    private float _fadeDurationMs;
    private float _targetVolume;
    private float _previousVolume;

    /// <summary>
    /// Gets or sets the target volume to fade towards.
    /// </summary>
    public float TargetVolume
    {
        get => _targetVolume;
        set
        {
            if (_targetVolume == value) return; // Same target, don't reset counter
            _previousVolume = _targetVolume;
            _targetVolume = value;
            _fadeSamplePosition = 0.0f; // Reset position for new target
        }
    }

    /// <summary>
    /// Gets or sets the fade duration in milliseconds.
    /// </summary>
    public float FadeDurationMs
    {
        get => _fadeDurationMs;
        set
        {
            _fadeDurationMs = value;
            var newSamples = (int)(_fadeDurationMs * _source.WaveFormat.SampleRate / 1000);
            
            // Make sure we don't overshoot the target when lerping
            if (newSamples < _fadeSamplePosition)
            {
                _fadeSamplePosition = newSamples;
            }
            _fadeSamples = newSamples;
        }
    }

    /// <inheritdoc/>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmoothVolumeSampleProvider"/> class.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="fadeDurationMs">The fade duration in milliseconds.</param>
    public SmoothVolumeSampleProvider(ISampleProvider source, float fadeDurationMs)
    {
        _source = source;
        _fadeSamplePosition = 0.0f;
        FadeDurationMs = fadeDurationMs;
    }

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        int sample = 0;
        
        while (sample < read)
        {
            var value = Lerp(_previousVolume, _targetVolume, _fadeSamplePosition / _fadeSamples);
            
            for (int ch = 0; ch < _source.WaveFormat.Channels; ch++)
            {
                buffer[offset + sample++] *= value;
            }
            
            // Don't overshoot the fade duration
            if (_fadeSamplePosition <= _fadeSamples)
            {
                _fadeSamplePosition++;
            }
        }
        
        return read;
    }

    /// <summary>
    /// Linearly interpolates between two values.
    /// </summary>
    private static float Lerp(float current, float target, float by)
    {
        return current * (1 - by) + target * by;
    }
}

