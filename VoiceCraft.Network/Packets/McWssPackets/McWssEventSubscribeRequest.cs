using System;
using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class McWssEventSubscribeRequest : McWssPacket<McWssEventSubscribeRequest.McWssEventSubscribeRequestBody>
    {
        public McWssEventSubscribeRequest(string eventName = "")
        {
            EventName = eventName;
            header.requestId = Guid.NewGuid().ToString();
            header.messagePurpose = "subscribe";
        }

        public override McWssEventSubscribeRequestBody body { get; set; } = new();

        [JsonIgnore]
        public string EventName
        {
            get => body.eventName;
            set => body.eventName = value;
        }

        //Resharper disable All
        public class McWssEventSubscribeRequestBody
        {
            public string eventName { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
}