using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateComponentPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public int NetworkId { get; set; }
        public ComponentType ComponentType { get; set; } = ComponentType.Unknown;
        public byte[]? Data { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put((byte)ComponentType);
            writer.Put(Data?.Length ?? 0);
            if (Data != null && Data.Length > 0)
                writer.Put(Data, 0, Data.Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            ComponentType = (ComponentType)reader.GetByte();
            var dataLength = reader.GetInt();
            if (dataLength <= 0) return;
            Data = new byte[dataLength];
            reader.GetBytes(Data, dataLength);
        }
    }
}