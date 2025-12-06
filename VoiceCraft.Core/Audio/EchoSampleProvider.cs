using NAudio.Wave;

namespace VoiceCraft.Core.Audio;

/// <summary>
/// A sample provider that adds echo/reverb effect to audio by using a delay buffer with feedback.
/// </summary>
/// <remarks>
/// Credits: https://www.youtube.com/watch?v=ZiIJRvNx2N0
/// </remarks>
public class EchoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _delayBuffer;
    private readonly int _delayLength;
    private int _delayPos;

    /// <summary>
    /// Gets or sets the echo mix amount (0 = dry, 1 = fully wet).
    /// </summary>
    public float EchoFactor { get; set; }

    /// <summary>
    /// Gets or sets the feedback decay factor (controls echo persistence).
    /// </summary>
    public float DecayFactor { get; set; }

    /// <inheritdoc/>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="EchoSampleProvider"/> class.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="echoDelayMs">The echo delay in milliseconds.</param>
    public EchoSampleProvider(ISampleProvider source, int echoDelayMs = 50)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        
        // Calculate delay in samples
        int channels = source.WaveFormat.Channels;
        _delayLength = (int)((echoDelayMs / 1000.0) * source.WaveFormat.SampleRate * channels);

        // Ensure alignment with channels to prevent channel bleeding
        if (channels > 0 && _delayLength % channels != 0)
        {
            _delayLength -= (_delayLength % channels);
        }
        
        // Minimum 1 frame
        if (_delayLength < channels) 
            _delayLength = channels;

        _delayBuffer = new float[_delayLength];
        _delayPos = 0;
        EchoFactor = 0.0f;
        DecayFactor = 0.8f;
    }

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        int read = _source.Read(buffer, offset, count);
        
        // Skip processing if echo is disabled
        if (EchoFactor == 0) 
            return read;

        // Local copies for thread safety (tearing prevention)
        float ef = EchoFactor;
        float df = DecayFactor;

        for (int i = 0; i < read; i++)
        {
            float input = buffer[offset + i];
            float delaySample = _delayBuffer[_delayPos];

            float output = input + (delaySample * ef);

            // Hard clamp for safety
            output = Math.Clamp(output, -1.0f, 1.0f);
            buffer[offset + i] = output;

            // Simple feedback loop - feed back the wet signal
            float feedback = Math.Clamp(output * df, -1.0f, 1.0f);
            _delayBuffer[_delayPos] = feedback;

            _delayPos++;
            if (_delayPos >= _delayLength)
            {
                _delayPos = 0;
            }
        }

        return read;
    }
}
