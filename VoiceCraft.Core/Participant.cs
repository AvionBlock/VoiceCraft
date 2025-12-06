namespace VoiceCraft.Core;

/// <summary>
/// Abstract base class representing a participant in the voice chat system.
/// </summary>
public abstract class Participant
{
    /// <summary>
    /// Gets or sets the display name of the participant.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets whether the participant is deafened (cannot hear others).
    /// </summary>
    public bool Deafened { get; set; }
    
    /// <summary>
    /// Gets or sets whether the participant is muted (cannot speak).
    /// </summary>
    public bool Muted { get; set; }
    
    /// <summary>
    /// Gets or sets the tick count when this participant last spoke.
    /// Used for voice activity detection and UI indicators.
    /// </summary>
    public long LastSpoke { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Participant"/> class.
    /// </summary>
    /// <param name="name">The display name of the participant.</param>
    protected Participant(string name)
    {
        Name = name;
    }
}

