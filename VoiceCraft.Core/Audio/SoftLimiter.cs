using NAudio.Wave;

namespace VoiceCraft.Core.Audio;

/// <summary>
/// A soft clipper/limiter audio effect that prevents harsh clipping while limiting peaks.
/// </summary>
/// <remarks>
/// Credits: https://www.markheath.net/post/limit-audio-naudio
/// </remarks>
public class SoftLimiter : Effect
{
    /// <inheritdoc/>
    public override string Name => "Soft Clipper/Limiter";

    /// <summary>
    /// Gets the boost parameter (0-18 dB).
    /// </summary>
    public EffectParameter Boost { get; } = new(0f, 0f, 18f, "Boost");
    
    /// <summary>
    /// Gets the brickwall limiter threshold parameter (-3 to 1 dB).
    /// </summary>
    public EffectParameter Brickwall { get; } = new(-0.1f, -3.0f, 1f, "Output Brickwall(dB)");

    private const float AmpDb = 8.6562f;
    private const float BaselineThresholdDb = -9f;
    private const float CurveA = 1.017f;
    private const float CurveB = -0.025f;
    
    private float _boostDb;
    private float _limitDb;
    private float _thresholdDb;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftLimiter"/> class.
    /// </summary>
    /// <param name="source">The audio source to process.</param>
    public SoftLimiter(ISampleProvider source) : base(source)
    {
        RegisterParameters(Boost, Brickwall);
    }

    /// <inheritdoc/>
    protected override void ParamsChanged()
    {
        _boostDb = Boost.CurrentValue;
        _limitDb = Brickwall.CurrentValue;
        _thresholdDb = BaselineThresholdDb + _limitDb;
    }

    /// <inheritdoc/>
    protected override void Sample(ref float spl0, ref float spl1)
    {
        // Add epsilon to avoid Log(0) which is -Infinity
        var dB0 = AmpDb * Math.Log(Math.Abs(spl0) + 1e-9) + _boostDb;
        var dB1 = AmpDb * Math.Log(Math.Abs(spl1) + 1e-9) + _boostDb;

        if (dB0 > _thresholdDb)
        {
            var overDb = (float)(dB0 - _thresholdDb);
            overDb = CurveA * overDb + CurveB * overDb * overDb;
            dB0 = Math.Min(_thresholdDb + overDb, _limitDb);
        }

        if (dB1 > _thresholdDb)
        {
            var overDb = (float)(dB1 - _thresholdDb);
            overDb = CurveA * overDb + CurveB * overDb * overDb;
            dB1 = Math.Min(_thresholdDb + overDb, _limitDb);
        }

        spl0 = (float)(Math.Exp(dB0 / AmpDb) * Math.Sign(spl0));
        spl1 = (float)(Math.Exp(dB1 / AmpDb) * Math.Sign(spl1));
    }
}
