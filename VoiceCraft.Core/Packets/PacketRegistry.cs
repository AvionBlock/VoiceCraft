using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Core.Packets
{
    public class PacketRegistry
    {
        private ConcurrentDictionary<byte, Type> RegisteredPackets = new();

        /// <summary>
        /// Registers a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <param name="Type">The type to create for the data to be parsed.</param>
        /// <param name="IsReliable"></param>
        public void RegisterPacket(byte Id, Type PacketType)
        {
            if (typeof(VoiceCraftPacket).IsAssignableFrom(PacketType) || typeof(MCCommPacket).IsAssignableFrom(PacketType) || typeof(CustomClientPacket).IsAssignableFrom(PacketType))
            {
                RegisteredPackets.AddOrUpdate(Id, PacketType, (key, old) => old = PacketType);
            }
            else
            {
                throw new ArgumentException($"PacketType needs to inherit from {nameof(VoiceCraftPacket)}, {nameof(MCCommPacket)} or {nameof(CustomClientPacket)}", nameof(PacketType));
            }
        }

        /// <summary>
        /// Deregisters a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <returns>The deregistered packet type.</returns>
        public Type? DeregisterPacket(byte Id)
        {
            if (RegisteredPackets.TryRemove(Id, out var packet)) return packet;
            return null;
        }

        /// <summary>
        /// Deregisters all registered packets.
        /// </summary>
        public void DeregisterAll()
        {
            RegisteredPackets.Clear();
        }

        /// <summary>
        /// Converts a packet from a byte array to the object.
        /// </summary>
        /// <param name="dataStream">The raw data.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public VoiceCraftPacket GetPacketFromDataStream(byte[] dataStream)
        {
            var PacketId = dataStream[0]; //This is the ID.

            if (!RegisteredPackets.TryGetValue(PacketId, out var packetType))
                throw new InvalidOperationException($"Invalid packet id {PacketId}");

            VoiceCraftPacket packet = GetPacketFromType(packetType);
            packet.ReadPacket(ref dataStream, 1); //Offset by 1 byte so we completely remove reading the Id.

            return packet;
        }

        /// <summary>
        /// Convert's a packet from a byte array to the object.
        /// </summary>
        /// <param name="dataStream">The raw data.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public CustomClientPacket GetCustomPacketFromDataStream(byte[] dataStream)
        {
            var PacketId = dataStream[0]; //This is the ID.

            if (!RegisteredPackets.TryGetValue(PacketId, out var packetType))
                throw new InvalidOperationException($"Invalid packet id {PacketId}");

            CustomClientPacket packet = GetCustomPacketFromType(packetType);
            packet.ReadPacket(ref dataStream, 1); //Offset by 1 byte so we completely remove reading the Id.

            return packet;
        }

        /// <summary>
        /// Converts a packet from a json string to the object.
        /// </summary>
        /// <param name="data">The raw data.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public MCCommPacket GetPacketFromJsonString(string data)
        {
            var jObject = JObject.Parse(data);
            var PacketId = jObject["PacketId"]?.Value<byte>() ?? byte.MaxValue; //Packet Id.

            if (!RegisteredPackets.TryGetValue(PacketId, out var packetType))
                throw new InvalidOperationException($"Invalid packet id {PacketId}");

            var packet = jObject.ToObject(packetType) as MCCommPacket;
            if (packet == null) throw new Exception("Could not create packet instance.");

            return packet;
        }

        /// <summary>
        /// Create's a packet from the type.
        /// </summary>
        /// <param name="PacketType">The packet type.</param>
        /// <returns>The packet</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static VoiceCraftPacket GetPacketFromType(Type PacketType)
        {
            if (!typeof(VoiceCraftPacket).IsAssignableFrom(PacketType))
                throw new ArgumentException($"PacketType needs to inherit from {nameof(VoiceCraftPacket)}", nameof(PacketType));

            var packet = Activator.CreateInstance(PacketType);
            if (packet == null) throw new Exception("Could not create packet instance.");

            return (VoiceCraftPacket)packet;
        }

        /// <summary>
        /// Create's a packet from the type.
        /// </summary>
        /// <param name="PacketType">The packet type.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static CustomClientPacket GetCustomPacketFromType(Type PacketType)
        {
            if (!typeof(CustomClientPacket).IsAssignableFrom(PacketType))
                throw new ArgumentException($"PacketType needs to inherit from {nameof(CustomClientPacket)}", nameof(PacketType));

            var packet = Activator.CreateInstance(PacketType);
            if (packet == null) throw new Exception("Could not create packet instance.");

            return (CustomClientPacket)packet;
        }
    }
}
