using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityEffectBitmaskUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityEffectBitmaskUpdatedPacket(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityEffectBitmaskUpdated;

        public int Id { get; private set; }
        public ushort Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetUShort();
        }
        
        public VcOnEntityEffectBitmaskUpdatedPacket Set(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}