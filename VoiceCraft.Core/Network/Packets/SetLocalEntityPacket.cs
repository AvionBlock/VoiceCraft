using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetLocalEntityPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetLocalEntity;
        public uint NetworkId { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetUInt();
        }
    }
}