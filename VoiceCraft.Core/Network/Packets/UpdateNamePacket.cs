using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class UpdateNamePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.UpdateName;
        public int NetworkId { get; set; }
        public string Name { get; set; }
        

        public UpdateNamePacket(int networkId, string name)
        {
            NetworkId = networkId;
            Name = name;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Name);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Name = reader.GetString();
        }
    }
}