using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class InfoPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Info;
        public string Motd { get; set; } = string.Empty;
        public int Clients { get; set; }
        public bool Discovery  { get; set; }
        public PositioningType PositioningType { get; set; }
        public int Tick { get; set; }
        
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Motd);
            writer.Put(Clients);
            writer.Put(Discovery);
            writer.Put((byte)PositioningType);
            writer.Put(Tick);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Motd = reader.GetString();
            Clients = reader.GetInt();
            Discovery = reader.GetBool();
            PositioningType = (PositioningType)reader.GetByte();
            Tick = reader.GetInt();
        }
    }
}