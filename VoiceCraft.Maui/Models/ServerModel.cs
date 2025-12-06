using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Maui.Models;

/// <summary>
/// Observable model representing a saved server configuration.
/// </summary>
public partial class ServerModel : ObservableObject, ICloneable
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _iP = string.Empty;

    [ObservableProperty]
    private int _port = 9050;

    [ObservableProperty]
    private short _key;

    /// <inheritdoc/>
    public object Clone() => MemberwiseClone();
}

