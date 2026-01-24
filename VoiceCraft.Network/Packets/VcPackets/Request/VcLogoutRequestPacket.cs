using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcLogoutRequestPacket(string reason) : IVoiceCraftPacket
{
    public VcLogoutRequestPacket() : this(string.Empty)
    {
    }

    public string Reason { get; private set; } = reason;

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