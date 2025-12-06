namespace VoiceCraft.Core;

/// <summary>
/// Specifies the source of player position data.
/// </summary>
public enum PositioningTypes
{
    /// <summary>
    /// Position data is provided by the server (Minecraft plugin).
    /// </summary>
    ServerSided,

    /// <summary>
    /// Position data is provided by the client (WebSocket connection).
    /// </summary>
    ClientSided,

    /// <summary>
    /// Position source is unknown or not yet determined.
    /// </summary>
    Unknown
}

