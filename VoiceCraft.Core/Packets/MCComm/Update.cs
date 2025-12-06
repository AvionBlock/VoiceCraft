using System.Collections.ObjectModel;
using System.Numerics;

namespace VoiceCraft.Core.Packets.MCComm;

public class Update : MCCommPacket
{
    public override byte PacketId => (byte)MCCommPacketTypes.Update;
    public Collection<Player> Players { get; } = new Collection<Player>();
}

public class Player
{
    public string PlayerId { get; set; } = string.Empty;
    public string DimensionId { get; set; } = string.Empty;
    // CA1805: Remove explicit initialization to default
    public Vector3 Location { get; set; }
    public float Rotation { get; set; }
    public float EchoFactor { get; set; }
    public bool Muffled { get; set; }
    public bool IsDead { get; set; }
}
