using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McHttpPackets;

public class McHttpUpdatePacket
{
    public List<string> Packets { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(McHttpUpdatePacket), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class McHttpUpdatePacketGenerationContext : JsonSerializerContext;