using System.Text.Json.Nodes;

namespace VoiceCraft.Network.Packets.McWssPackets;

public class McWssGenericPacket : McWssPacket<JsonObject>
{
    public override JsonObject body { get; set; } = new();
}