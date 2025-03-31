using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemoveEffectPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveEffect;
        
        public byte Index { get; private set; }

        public RemoveEffectPacket(byte index = 0)
        {
            Index = index;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Index);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Index = reader.GetByte();
        }
    }
}