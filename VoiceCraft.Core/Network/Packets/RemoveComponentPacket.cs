using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveComponentPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public uint NetworkId { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(ComponentType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetUInt();
            ComponentType = reader.GetString();
        }
    }
}