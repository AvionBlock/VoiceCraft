using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateListenBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.UpdateListenBitmask;
        public int NetworkId { get; set; }
        public ulong Bitmask { get; set; }

        public UpdateListenBitmaskPacket(int networkId, ulong bitmask)
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