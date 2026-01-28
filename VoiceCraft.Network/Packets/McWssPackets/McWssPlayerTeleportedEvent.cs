using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class McWssPlayerTeleportedEvent : McWssPacket<McWssPlayerTeleportedEvent.McWssPlayerTeleportedEventBody>
    {
        public override McWssPlayerTeleportedEventBody body { get; set; } = new();

        //Resharper disable all
        public class McWssPlayerTeleportedEventBody
        {
            public int cause { get; set; }
            public int itemType { get; set; }
            public float metersTravelled { get; set; }
            public Player player { get; set; } = new Player();
        }

        public class Player
        {
            public string color { get; set; } = string.Empty;
            public int dimension { get; set; }
            public long id { get; set; }
            public string name { get; set; } = string.Empty;
            public Position position { get; set; }
            public string type { get; set; } = string.Empty;
            public long variant { get; set; }
            public float yRot { get; set; }
        }

        public struct Position
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }
        //Resharper enable all
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(McWssPlayerTeleportedEvent), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class McWssPlayerTeleportedEventGenerationContext : JsonSerializerContext;
}