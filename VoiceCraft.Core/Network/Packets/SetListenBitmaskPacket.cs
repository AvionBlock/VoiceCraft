using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetListenBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetListenBitmask;
        public byte Id { get; private set; }
        public ulong Bitmask { get; private set; }

        public SetListenBitmaskPacket(byte id = 0, ulong bitmask = 0)
        {
            Id = id;
            Bitmask = bitmask;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Bitmask);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Bitmask = reader.GetULong();
        }
    }
}