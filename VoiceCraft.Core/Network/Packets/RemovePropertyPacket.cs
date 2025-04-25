using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class RemovePropertyPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.RemoveProperty;
        public int Id { get; private set; }
        public string Key { get; private set; }

        public RemovePropertyPacket(int id = 0, string key = "")
        {
            Id = id;
            Key = key;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Key, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Key = reader.GetString(Constants.MaxStringLength);
        }
    }
}