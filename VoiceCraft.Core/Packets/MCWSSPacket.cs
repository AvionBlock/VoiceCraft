using Newtonsoft.Json;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Core.Packets;

/// <summary>
/// Generic wrapper class for Minecraft WebSocket packets.
/// Encapsulates a header and typed body for Bedrock Edition WebSocket communication.
/// </summary>
/// <typeparam name="T">The body type for this packet.</typeparam>
public class MCWSSPacket<T> where T : new()
{
    /// <summary>
    /// Gets or sets the packet header containing message metadata.
    /// </summary>
    public Header header { get; set; }

    /// <summary>
    /// Gets or sets the packet body containing the actual data.
    /// </summary>
    public T body { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MCWSSPacket{T}"/> class.
    /// </summary>
    public MCWSSPacket()
    {
        header = new Header();
        body = new T();
    }

    /// <summary>
    /// Serializes this packet to a JSON string.
    /// </summary>
    /// <returns>The JSON representation of this packet.</returns>
    public string SerializePacket() => JsonConvert.SerializeObject(this);
}

