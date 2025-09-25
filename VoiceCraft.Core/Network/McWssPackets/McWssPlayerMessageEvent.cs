using System.Text.Json.Serialization;

namespace VoiceCraft.Core.Network.McWssPackets
{
    public class McWssPlayerMessageEvent : McWssPacket<McWssPlayerMessageEvent.McWssPlayerMessageBody>
    {
        public override McWssPlayerMessageBody body { get; set; } = new McWssPlayerMessageBody();

        [JsonIgnore]
        public string Message
        {
            get => body.message;
            set => body.message = value;
        }

        [JsonIgnore]
        public string Receiver
        {
            get => body.receiver;
            set => body.receiver = value;
        }

        [JsonIgnore]
        public string Sender
        {
            get => body.sender;
            set => body.sender = value;
        }

        [JsonIgnore]
        public string Type
        {
            get => body.type;
            set => body.type = value;
        }

        //Resharper disable All
        public class McWssPlayerMessageBody
        {
            public string message { get; set; } = string.Empty;
            public string receiver { get; set; } = string.Empty;
            public string sender { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
}