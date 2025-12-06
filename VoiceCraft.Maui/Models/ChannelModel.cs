using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core;

namespace VoiceCraft.Maui.Models;

/// <summary>
/// Observable model representing a voice channel.
/// </summary>
public partial class ChannelModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _requiresPassword;

    [ObservableProperty]
    private bool _joined;

    [ObservableProperty]
    private Channel _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelModel"/> class.
    /// </summary>
    /// <param name="channel">The channel data.</param>
    public ChannelModel(Channel channel)
    {
        _channel = channel;
        _name = channel.Name;
        _requiresPassword = !string.IsNullOrWhiteSpace(channel.Password);
    }
}

