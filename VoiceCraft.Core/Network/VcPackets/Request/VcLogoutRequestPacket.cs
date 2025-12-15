using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcLogoutRequestPacket : IVoiceCraftPacket
    {
        public VcLogoutRequestPacket(): this(string.Empty)
        {
        }
        
        public VcLogoutRequestPacket(string reason)
        {
            Reason = reason;
        }

        public VcPacketType PacketType => VcPacketType.LogoutRequest;

        public string Reason { get; private set; }
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Reason, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Reason = reader.GetString(Constants.MaxStringLength);
        }
        
        public VcLogoutRequestPacket Set(string reason = "")
        {
            Reason = reason;
            return this;
        }
    }
}