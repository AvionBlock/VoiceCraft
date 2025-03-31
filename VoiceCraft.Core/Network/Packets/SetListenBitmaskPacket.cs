using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetListenBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetListenBitmask;
        public int NetworkId { get; private set; }
        public ulong Bitmask { get; private set; }

        public SetListenBitmaskPacket(int networkId = 0, ulong bitmask = 0)
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