using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class InfoPacket : VoiceCraftPacket
    {
        public InfoPacket(string motd = "", int clients = 0, PositioningType positioningType = PositioningType.Server,
            int tick = 0)
        {
            Motd = motd;
            Clients = clients;
            PositioningType = positioningType;
            Tick = tick;
        }

        public override PacketType PacketType => PacketType.Info;

        public string Motd { get; private set; }
        public int Clients { get; private set; }
        public PositioningType PositioningType { get; private set; }
        public int Tick { get; private set; }


        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Motd, Constants.MaxStringLength);
            writer.Put(Clients);
            writer.Put((byte)PositioningType);
            writer.Put(Tick);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Motd = reader.GetString(Constants.MaxStringLength);
            Clients = reader.GetInt();
            PositioningType = (PositioningType)reader.GetByte();
            Tick = reader.GetInt();
        }
    }
}