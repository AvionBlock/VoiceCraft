using System;

namespace VoiceCraft.Core.Network.VcPackets
{
    public interface IVoiceCraftRIdPacket
    {
        Guid RequestId { get; }
    }
}