namespace VoiceCraft.Core.Packets.MCWSS;

/// <summary>
/// Event data for player travel events from Minecraft.
/// </summary>
public class PlayerTravelled
{
    /// <summary>Gets or sets whether the player is underwater.</summary>
    public bool isUnderwater { get; set; }

    /// <summary>Gets or sets the distance travelled in meters.</summary>
    public float metersTravelled { get; set; }

    /// <summary>Gets or sets the new biome ID.</summary>
    public int newBiome { get; set; }

    /// <summary>Gets or sets the player information.</summary>
    public Player player { get; set; } = new();

    /// <summary>Gets or sets the travel method (walking, swimming, etc.).</summary>
    public int travelMethod { get; set; }
}

/// <summary>
/// Player information from Minecraft events.
/// </summary>
public class Player
{
    /// <summary>Gets or sets the player color.</summary>
    public string color { get; set; } = string.Empty;

    /// <summary>Gets or sets the dimension ID.</summary>
    public int dimension { get; set; }

    /// <summary>Gets or sets the player entity ID.</summary>
    public long id { get; set; }

    /// <summary>Gets or sets the player name/gamertag.</summary>
    public string name { get; set; } = string.Empty;

    /// <summary>Gets or sets the player position.</summary>
    public Position position { get; set; }

    /// <summary>Gets or sets the player type.</summary>
    public string type { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant ID.</summary>
    public long variant { get; set; }

    /// <summary>Gets or sets the Y rotation in degrees.</summary>
    public float yRot { get; set; }
}

/// <summary>
/// 3D position coordinates.
/// </summary>
public struct Position
{
    /// <summary>Gets or sets the X coordinate.</summary>
    public float x { get; set; }

    /// <summary>Gets or sets the Y coordinate.</summary>
    public float y { get; set; }

    /// <summary>Gets or sets the Z coordinate.</summary>
    public float z { get; set; }
}

