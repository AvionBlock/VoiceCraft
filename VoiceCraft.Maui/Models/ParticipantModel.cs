using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Maui.VoiceCraft;

namespace VoiceCraft.Maui.Models;

/// <summary>
/// Observable model representing a voice participant.
/// </summary>
public partial class ParticipantModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSpeaking;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isDeafened;

    [ObservableProperty]
    private float _volume;

    [ObservableProperty]
    private VoiceCraftParticipant _participant;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticipantModel"/> class.
    /// </summary>
    /// <param name="participant">The participant data.</param>
    public ParticipantModel(VoiceCraftParticipant participant)
    {
        _participant = participant;
        _isMuted = participant.Muted;
        _isDeafened = participant.Deafened;
        _volume = participant.Volume;
    }

    partial void OnVolumeChanged(float value)
    {
        Participant.Volume = value;
    }
}

