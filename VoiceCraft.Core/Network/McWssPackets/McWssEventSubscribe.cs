using System.Text.Json.Serialization;

namespace VoiceCraft.Core.Network.McWssPackets
{
    public class McWssEventSubscribe : McWssPacket<McWssEventSubscribe.McWssEventSubscribeBody>
    {
        public override McWssEventSubscribeBody body { get; set; } = new McWssEventSubscribeBody();

        [JsonIgnore]
        public string EventName
        {
            get => body.eventName;
            set => body.eventName = value;
        }

        public McWssEventSubscribe(string eventName = "")
        {
            EventName = eventName;
            header.messagePurpose = "subscribe";
        }
        
        //Resharper disable All
        public class McWssEventSubscribeBody
        {
            public string eventName { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
}