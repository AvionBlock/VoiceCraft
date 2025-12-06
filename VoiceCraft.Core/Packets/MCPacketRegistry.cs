using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Core.Packets;

/// <summary>
/// Registry for Minecraft WebSocket (MCWSS) packet types.
/// Manages packet registration and JSON deserialization for Bedrock Edition WebSocket communication.
/// </summary>
public class MCPacketRegistry
{
    private readonly ConcurrentDictionary<Header, Type> _registeredPackets = new();

    /// <summary>
    /// Registers a packet type with the specified header.
    /// </summary>
    /// <param name="header">The packet header for identification.</param>
    /// <param name="packetType">The packet type to register.</param>
    public void RegisterPacket(Header header, Type packetType)
    {
        _registeredPackets.AddOrUpdate(header, packetType, (_, _) => packetType);
    }

    /// <summary>
    /// Deregisters a packet by header.
    /// </summary>
    /// <param name="header">The packet header.</param>
    /// <returns>The deregistered packet type, or null if not found.</returns>
    public Type? DeregisterPacket(Header header)
    {
        return _registeredPackets.TryRemove(header, out var packet) ? packet : null;
    }

    /// <summary>
    /// Deregisters all registered packets.
    /// </summary>
    public void DeregisterAll()
    {
        _registeredPackets.Clear();
    }

    /// <summary>
    /// Deserializes a packet from a JSON string.
    /// </summary>
    /// <param name="data">The JSON data.</param>
    /// <returns>The deserialized packet object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when packet header is not registered.</exception>
    public object GetPacketFromJsonString(string data)
    {
        var header = JObject.Parse(data)["header"]?.ToObject<Header>() ?? new Header();

        if (!_registeredPackets.TryGetValue(header, out var packetType))
            throw new InvalidOperationException($"Invalid packet header {header}");

        return GetMCWSSPacketFromType(data, packetType);
    }

    /// <summary>
    /// Deserializes a packet from JSON data into the specified type.
    /// </summary>
    /// <param name="data">The JSON data.</param>
    /// <param name="packetType">The target packet type.</param>
    /// <returns>The deserialized packet object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static object GetMCWSSPacketFromType(string data, Type packetType)
    {
        var packet = JsonConvert.DeserializeObject(data, packetType)
            ?? throw new InvalidOperationException("Could not create packet instance");

        return packet;
    }
}

