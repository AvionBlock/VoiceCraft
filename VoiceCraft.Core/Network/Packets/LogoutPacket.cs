using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LogoutPacket : VoiceCraftPacket
    {
        public LogoutPacket(string reason = "")
        {
            Reason = reason;
        }

        public override PacketType PacketType => PacketType.Logout;

        public string Reason { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Reason, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Reason = reader.GetString(Constants.MaxStringLength);
        }
    }
}