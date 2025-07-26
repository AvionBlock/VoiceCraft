using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiDenyPacket : McApiPacket
    {
        public override McApiPacketType PacketType => McApiPacketType.Deny;
        public string ReasonKey { get; private set; }

        public McApiDenyPacket(string reasonKey = "")
        {
            ReasonKey = reasonKey;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(ReasonKey, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            ReasonKey = reader.GetString(Constants.MaxStringLength);
        }
    }
}