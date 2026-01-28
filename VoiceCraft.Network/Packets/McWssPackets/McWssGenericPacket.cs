using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets;

public class McWssGenericPacket : McWssPacket<JsonObject>
{
    public override JsonObject body { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(McWssGenericPacket), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class McWssGenericPacketGenerationContext : JsonSerializerContext;