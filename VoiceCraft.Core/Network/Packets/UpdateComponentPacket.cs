using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateComponentPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveComponent;
        public uint NetworkId { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        public byte[]? Data { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(ComponentType);
            writer.Put(Data?.Length ?? 0);
            if (Data != null && Data.Length > 0)
                writer.Put(Data, 0, Data.Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetUInt();
            ComponentType = reader.GetString();
            var dataLength = reader.GetInt();
            if (dataLength <= 0) return;
            Data = new byte[dataLength];
            reader.GetBytes(Data, dataLength);
        }
    }
}