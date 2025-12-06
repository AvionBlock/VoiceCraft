using NAudio.Wave;
using System;

namespace VoiceCraft.Core.Audio;

/// <summary>
/// Abstract base class for audio effects that process sample data.
/// Provides common DSP helper methods and parameter management.
/// </summary>
/// <remarks>
/// Credits: https://www.markheath.net/post/limit-audio-naudio
/// </remarks>
public abstract class Effect : ISampleProvider
{
    private readonly ISampleProvider _source;
    private bool _paramsChanged;

    /// <summary>
    /// Gets the sample rate of the audio.
    /// </summary>
    public float SampleRate { get; }

    /// <summary>
    /// Gets the wave format of the source.
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Gets the display name of this effect.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class.
    /// </summary>
    /// <param name="source">The audio source to process.</param>
    protected Effect(ISampleProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        SampleRate = source.WaveFormat.SampleRate;
    }

    /// <summary>
    /// Registers effect parameters and sets up change notifications.
    /// </summary>
    /// <param name="parameters">The parameters to register.</param>
    protected void RegisterParameters(params EffectParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _paramsChanged = true;
        foreach (var param in parameters)
        {
            param.ValueChanged += (_, _) => _paramsChanged = true;
        }
    }

    /// <summary>
    /// Called when any registered parameter changes. Derived classes should recalculate cached values.
    /// </summary>
    protected abstract void ParamsChanged();

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (_paramsChanged)
        {
            ParamsChanged();
            _paramsChanged = false;
        }

        var samplesAvailable = _source.Read(buffer, offset, count);
        Block(samplesAvailable);

        if (WaveFormat.Channels == 1)
        {
            for (int n = 0; n < samplesAvailable; n++)
            {
                float right = 0.0f;
                Sample(ref buffer[offset + n], ref right);
            }
        }
        else if (WaveFormat.Channels == 2)
        {
            for (int n = 0; n < samplesAvailable; n += 2)
            {
                Sample(ref buffer[offset + n], ref buffer[offset + n + 1]);
            }
        }

        return samplesAvailable;
    }

    /// <summary>
    /// Called before each block is processed.
    /// </summary>
    /// <param name="samplesblock">Number of samples in this block.</param>
    public virtual void Block(int samplesblock)
    {
    }

    /// <summary>
    /// Process a single stereo sample pair.
    /// </summary>
    /// <param name="spl0">Left channel sample.</param>
    /// <param name="spl1">Right channel sample.</param>
    protected abstract void Sample(ref float spl0, ref float spl1);

    /// <inheritdoc/>
    public override string ToString() => Name;

    #region DSP Helper Methods
    /// <summary>Conversion factor: 20 / ln(10)</summary>
    protected const float Log2Db = 8.6858896380650365530225783783321f;
    
    /// <summary>Conversion factor: ln(10) / 20</summary>
    protected const float Db2Log = 0.11512925464970228420089957273422f;

    /// <summary>Returns the smaller of two floats.</summary>
    protected static float Min(float a, float b) => Math.Min(a, b);
    
    /// <summary>Returns the larger of two floats.</summary>
    protected static float Max(float a, float b) => Math.Max(a, b);
    
    /// <summary>Returns the absolute value.</summary>
    protected static float Abs(float a) => Math.Abs(a);
    
    /// <summary>Returns e raised to a power.</summary>
    protected static float Exp(float a) => (float)Math.Exp(a);
    
    /// <summary>Returns the square root.</summary>
    protected static float Sqrt(float a) => (float)Math.Sqrt(a);
    
    /// <summary>Returns the sine.</summary>
    protected static float Sin(float a) => (float)Math.Sin(a);
    
    /// <summary>Returns the tangent.</summary>
    protected static float Tan(float a) => (float)Math.Tan(a);
    
    /// <summary>Returns the cosine.</summary>
    protected static float Cos(float a) => (float)Math.Cos(a);
    
    /// <summary>Returns a raised to power b.</summary>
    protected static float Pow(float a, float b) => (float)Math.Pow(a, b);
    
    /// <summary>Returns the sign of a number.</summary>
    protected static float Sign(float a) => Math.Sign(a);
    
    /// <summary>Returns the natural logarithm.</summary>
    protected static float Log(float a) => (float)Math.Log(a);
    
    /// <summary>Gets the value of PI.</summary>
    protected static float PI => (float)Math.PI;
    #endregion
}
