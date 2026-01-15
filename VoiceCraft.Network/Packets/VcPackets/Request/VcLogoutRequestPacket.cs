using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcLogoutRequestPacket : IVoiceCraftPacket
{
    public VcLogoutRequestPacket() : this(string.Empty)
    {
    }

    public VcLogoutRequestPacket(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; private set; }

    public VcPacketType PacketType => VcPacketType.LogoutRequest;

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