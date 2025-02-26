using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetLocalEntityPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetLocalEntity;
        public uint Id { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetUInt();
        }
    }
}