using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class InfoPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Info;
        [StringLength(Constants.MaxStringLength)]
        public string Motd { get; private set; }
        public int Clients { get; private set; }
        public bool Discovery  { get; private set; }
        public PositioningType PositioningType { get; private set; }
        public int Tick { get; private set; }

        public InfoPacket(string motd, int clients, bool discovery, PositioningType positioningType, int tick)
        {
            Motd = motd;
            Clients = clients;
            Discovery = discovery;
            PositioningType = positioningType;
            Tick = tick;
        }
        
        
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