namespace VoiceCraft.Core.Audio;

/// <summary>
/// Represents a configurable parameter for an audio effect with min/max bounds and change notification.
/// </summary>
/// <remarks>
/// Credits: https://www.markheath.net/post/limit-audio-naudio
/// </remarks>
public class EffectParameter
{
    private float _currentValue;

    /// <summary>
    /// Gets the minimum allowed value.
    /// </summary>
    public float Min { get; }

    /// <summary>
    /// Gets the maximum allowed value.
    /// </summary>
    public float Max { get; }

    /// <summary>
    /// Gets the description of this parameter.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Occurs when the value changes.
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Gets or sets the current value. Must be within Min and Max bounds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside bounds.</exception>
    public float CurrentValue
    {
        get => _currentValue;
        set
        {
            if (value < Min || value > Max)
                throw new ArgumentOutOfRangeException(nameof(CurrentValue), 
                    $"Value must be between {Min} and {Max}");
            
            if (_currentValue != value)
            {
                _currentValue = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectParameter"/> class.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <param name="description">A description of the parameter.</param>
    public EffectParameter(float defaultValue, float minimum, float maximum, string description)
    {
        Min = minimum;
        Max = maximum;
        Description = description;
        _currentValue = defaultValue; // Set directly to avoid event invocation
    }
}
