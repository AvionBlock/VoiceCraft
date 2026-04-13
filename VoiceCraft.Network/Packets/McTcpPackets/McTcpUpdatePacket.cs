using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McTcpPackets;

public class McTcpUpdatePacket
{
    public string Token { get; set; } = string.Empty;
    public List<string> Packets { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(McTcpUpdatePacket), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class McTcpUpdatePacketGenerationContext : JsonSerializerContext;
