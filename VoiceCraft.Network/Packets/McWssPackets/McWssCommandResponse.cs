using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class McWssCommandResponse : McWssPacket<McWssCommandResponse.McWssCommandResponseBody>
    {
        public override McWssCommandResponseBody body { get; set; } = new();

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
        public class McWssCommandResponseBody
        {
            public int statusCode { get; set; } = 0;
            public string statusMessage { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
}