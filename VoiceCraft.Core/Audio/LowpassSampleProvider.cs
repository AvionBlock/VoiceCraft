using NAudio.Dsp;
using NAudio.Wave;

namespace VoiceCraft.Core.Audio;

/// <summary>
/// A sample provider that applies a lowpass filter to audio, useful for simulating
/// muffled audio (e.g., when a player is underwater or behind walls).
/// </summary>
public class LowpassSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly BiQuadFilter[] _filters;
    private readonly int _channels;

    /// <summary>
    /// Gets or sets whether the lowpass filter is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="LowpassSampleProvider"/> class.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="cutOffFreq">The cutoff frequency in Hz.</param>
    /// <param name="bandWidth">The bandwidth (Q factor).</param>
    public LowpassSampleProvider(ISampleProvider source, int cutOffFreq, int bandWidth)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _channels = source.WaveFormat.Channels;
        _filters = new BiQuadFilter[_channels];

        for (int i = 0; i < _channels; i++)
        {
            _filters[i] = BiQuadFilter.LowPassFilter(source.WaveFormat.SampleRate, cutOffFreq, bandWidth);
        }
    }

    /// <inheritdoc/>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        int samplesRead = _source.Read(buffer, offset, count);

        if (Enabled)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                // Map sample index to channel index for interleaved audio
                // 0 -> Ch0, 1 -> Ch1, 2 -> Ch0, 3 -> Ch1 ...
                int ch = i % _channels;
                buffer[offset + i] = _filters[ch].Transform(buffer[offset + i]);
            }
        }

        return samplesRead;
    }
}
