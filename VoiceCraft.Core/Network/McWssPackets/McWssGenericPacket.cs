using System.Text.Json.Nodes;

namespace VoiceCraft.Core.Network.McWssPackets
{
    public class McWssGenericPacket : McWssPacket<JsonObject>
    {
        public override JsonObject body { get; set; } = new JsonObject();
    }
}