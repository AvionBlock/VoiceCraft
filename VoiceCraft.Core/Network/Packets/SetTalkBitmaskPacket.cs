using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTalkBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetTalkBitmask;
        public int NetworkId { get; set; }
        public ulong Bitmask { get; set; }

        public SetTalkBitmaskPacket(int networkId, ulong bitmask)
        {
            NetworkId = networkId;
            Bitmask = bitmask;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Bitmask);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Bitmask = reader.GetULong();
        }
    }
}