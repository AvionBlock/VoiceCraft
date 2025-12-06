namespace VoiceCraft.Core.Packets.MCWSS;

/// <summary>
/// Represents a command body for Minecraft WebSocket commands.
/// </summary>
public class Command
{
    /// <summary>Gets or sets the command line to execute.</summary>
    public string commandLine { get; set; } = string.Empty;

    /// <summary>Gets or sets the protocol version.</summary>
    public int version { get; set; } = 1;
}
