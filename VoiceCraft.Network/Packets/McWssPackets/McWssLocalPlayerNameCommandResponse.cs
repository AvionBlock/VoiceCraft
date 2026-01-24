using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class
        McWssLocalPlayerNameCommandResponse : McWssPacket<
        McWssLocalPlayerNameCommandResponse.McWssLocalPlayerNameCommandResponseBody>
    {
        public override McWssLocalPlayerNameCommandResponseBody body { get; set; } = new();

        [JsonIgnore]
        public string LocalPlayerName
        {
            get => body.localplayername;
            set => body.localplayername = value;
        }

        [JsonIgnore]
        public int StatusCode
        {
            get => body.statusCode;
            set => body.statusCode = value;
        }

        [JsonIgnore]
        public string StatusMessage
        {
            get => body.statusMessage;
            set => body.statusMessage = value;
        }

        //Resharper disable All
        public class McWssLocalPlayerNameCommandResponseBody
        {
            public string localplayername { get; set; } = string.Empty;
            public int statusCode { get; set; } = 0;
            public string statusMessage { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(McWssLocalPlayerNameCommandResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class McWssLocalPlayerNameCommandResponseGenerationContext : JsonSerializerContext;
}