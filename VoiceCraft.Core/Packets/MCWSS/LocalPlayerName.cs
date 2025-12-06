namespace VoiceCraft.Core.Packets.MCWSS;

/// <summary>
/// Response body containing the local player name from Minecraft.
/// </summary>
public class LocalPlayerName
{
    /// <summary>Gets or sets the local player gamertag.</summary>
    public string localplayername { get; set; } = string.Empty;

    /// <summary>Gets or sets the status code.</summary>
    public int statusCode { get; set; }

    /// <summary>Gets or sets the status message.</summary>
    public string statusMessage { get; set; } = string.Empty;
}

