using System;

namespace VoiceCraft.Network.Packets.VcPackets;

public interface IVoiceCraftRIdPacket
{
    Guid RequestId { get; }
}