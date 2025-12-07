using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Response
{
    public class VcInfoResponsePacket : IVoiceCraftPacket
    {
        public VcInfoResponsePacket() : this(string.Empty, 0, PositioningType.Server, 0)
        {
        }

        public VcInfoResponsePacket(string motd, int clients, PositioningType positioningType, int tick)
        {
            Motd = motd;
            Clients = clients;
            PositioningType = positioningType;
            Tick = tick;
        }

        public VcPacketType PacketType => VcPacketType.InfoResponse;

        public string Motd { get; private set; }
        public int Clients { get; private set; }
        public PositioningType PositioningType { get; private set; }
        public int Tick { get; private set; }


        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Motd, Constants.MaxStringLength);
            writer.Put(Clients);
            writer.Put((byte)PositioningType);
            writer.Put(Tick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Motd = reader.GetString(Constants.MaxStringLength);
            Clients = reader.GetInt();
            PositioningType = (PositioningType)reader.GetByte();
            Tick = reader.GetInt();
        }

        public VcInfoResponsePacket Set(string motd = "", int clients = 0,
            PositioningType positioningType = PositioningType.Server,
            int tick = 0)
        {
            Motd = motd;
            Clients = clients;
            PositioningType = positioningType;
            Tick = tick;
            return this;
        }
    }
}