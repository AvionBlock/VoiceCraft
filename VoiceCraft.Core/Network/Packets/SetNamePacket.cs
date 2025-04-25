using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetNamePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetName;
        public int Id { get; private set; }
        public string Name { get; private set; }
        

        public SetNamePacket(int id = 0, string name = "")
        {
            Id = id;
            Name = name;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Name, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Name = reader.GetString(Constants.MaxStringLength);
        }
    }
}