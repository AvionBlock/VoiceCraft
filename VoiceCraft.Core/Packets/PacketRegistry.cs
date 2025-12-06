using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace VoiceCraft.Core.Packets;

/// <summary>
/// Registry for managing packet types and deserialization.
/// Supports VoiceCraft, MCComm, and CustomClient packet types.
/// </summary>
public class PacketRegistry
{
    private readonly ConcurrentDictionary<byte, Type> _registeredPackets = new();

    /// <summary>
    /// Registers a packet type with the specified ID.
    /// </summary>
    /// <param name="id">The packet ID.</param>
    /// <param name="packetType">The packet type. Must inherit from VoiceCraftPacket, MCCommPacket, or CustomClientPacket.</param>
    /// <exception cref="ArgumentException">Thrown when packetType doesn't inherit from a valid base class.</exception>
    public void RegisterPacket(byte id, Type packetType)
    {
        if (!typeof(VoiceCraftPacket).IsAssignableFrom(packetType) &&
            !typeof(MCCommPacket).IsAssignableFrom(packetType) &&
            !typeof(CustomClientPacket).IsAssignableFrom(packetType))
        {
            throw new ArgumentException(
                $"PacketType must inherit from {nameof(VoiceCraftPacket)}, {nameof(MCCommPacket)}, or {nameof(CustomClientPacket)}",
                nameof(packetType));
        }

        _registeredPackets.AddOrUpdate(id, packetType, (_, _) => packetType);
    }

    /// <summary>
    /// Deregisters a packet by ID.
    /// </summary>
    /// <param name="id">The packet ID.</param>
    /// <returns>The deregistered packet type, or null if not found.</returns>
    public Type? DeregisterPacket(byte id)
    {
        return _registeredPackets.TryRemove(id, out var packet) ? packet : null;
    }

    /// <summary>
    /// Deregisters all registered packets.
    /// </summary>
    public void DeregisterAll()
    {
        _registeredPackets.Clear();
    }

    /// <summary>
    /// Deserializes a VoiceCraft packet from a byte array.
    /// </summary>
    /// <param name="dataStream">The raw data.</param>
    /// <returns>The deserialized packet.</returns>
    /// <exception cref="InvalidOperationException">Thrown when packet ID is not registered.</exception>
    public VoiceCraftPacket GetPacketFromDataStream(byte[] dataStream)
    {
        ArgumentNullException.ThrowIfNull(dataStream);
        byte packetId = dataStream[0];

        if (!_registeredPackets.TryGetValue(packetId, out var packetType))
            throw new InvalidOperationException($"Invalid packet id {packetId}");

        var packet = GetPacketFromType(packetType);
        // Skip PacketId (1 byte)
        packet.Read(dataStream.AsSpan(1));

        return packet;
    }

    /// <summary>
    /// Deserializes a CustomClient packet from a byte array.
    /// </summary>
    /// <param name="dataStream">The raw data.</param>
    /// <returns>The deserialized packet.</returns>
    /// <exception cref="InvalidOperationException">Thrown when packet ID is not registered.</exception>
    public CustomClientPacket GetCustomPacketFromDataStream(byte[] dataStream)
    {
        ArgumentNullException.ThrowIfNull(dataStream);
        byte packetId = dataStream[0];

        if (!_registeredPackets.TryGetValue(packetId, out var packetType))
            throw new InvalidOperationException($"Invalid packet id {packetId}");

        var packet = GetCustomPacketFromType(packetType);
        // Skip PacketId (1 byte)
        packet.Read(dataStream.AsSpan(1));

        return packet;
    }

    /// <summary>
    /// Deserializes an MCComm packet from a JSON string.
    /// </summary>
    /// <param name="data">The JSON data.</param>
    /// <returns>The deserialized packet.</returns>
    /// <exception cref="InvalidOperationException">Thrown when packet ID is not registered or deserialization fails.</exception>
    public MCCommPacket GetPacketFromJsonString(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var jObject = JObject.Parse(data);
        byte packetId = jObject["PacketId"]?.Value<byte>() ?? byte.MaxValue;

        if (!_registeredPackets.TryGetValue(packetId, out var packetType))
            throw new InvalidOperationException($"Invalid packet id {packetId}");

        var packet = jObject.ToObject(packetType) as MCCommPacket
            ?? throw new InvalidOperationException("Could not deserialize packet");

        return packet;
    }

    /// <summary>
    /// Creates a VoiceCraft packet instance from a type.
    /// </summary>
    /// <param name="packetType">The packet type.</param>
    /// <returns>The created packet.</returns>
    /// <exception cref="ArgumentException">Thrown when type doesn't inherit from VoiceCraftPacket.</exception>
    /// <exception cref="InvalidOperationException">Thrown when instance creation fails.</exception>
    public static VoiceCraftPacket GetPacketFromType(Type packetType)
    {
        if (!typeof(VoiceCraftPacket).IsAssignableFrom(packetType))
            throw new ArgumentException($"PacketType must inherit from {nameof(VoiceCraftPacket)}", nameof(packetType));

        var packet = Activator.CreateInstance(packetType) as VoiceCraftPacket
            ?? throw new InvalidOperationException("Could not create packet instance");

        return packet;
    }

    /// <summary>
    /// Creates a CustomClient packet instance from a type.
    /// </summary>
    /// <param name="packetType">The packet type.</param>
    /// <returns>The created packet.</returns>
    /// <exception cref="ArgumentException">Thrown when type doesn't inherit from CustomClientPacket.</exception>
    /// <exception cref="InvalidOperationException">Thrown when instance creation fails.</exception>
    public static CustomClientPacket GetCustomPacketFromType(Type packetType)
    {
        if (!typeof(CustomClientPacket).IsAssignableFrom(packetType))
            throw new ArgumentException($"PacketType must inherit from {nameof(CustomClientPacket)}", nameof(packetType));

        var packet = Activator.CreateInstance(packetType) as CustomClientPacket
            ?? throw new InvalidOperationException("Could not create packet instance");

        return packet;
    }
}
