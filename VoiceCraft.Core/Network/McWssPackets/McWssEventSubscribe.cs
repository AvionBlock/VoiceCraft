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
        }
        
        //Resharper disable All
        public class McWssEventSubscribeBody
        {
            public string eventName { get; set; } = string.Empty;
        }
        //Resharper enable All
    }

    /*
    private class McwssPlayerMessageStructure
    {
        public McwssEventHeaders header { get; set; } = new McwssEventHeaders();
        public McwssPlayerMessageBody body { get; set; } = new McwssPlayerMessageBody();
    }

    private class McwssPlayerMessageBody
    {
        public string message { get; set; } = string.Empty;
        public string receiver { get; set; } = string.Empty;
        public string sender { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    private class RawTextStructure
    {
        public RawTextMessage[] rawtext { get; set; } = Array.Empty<RawTextMessage>();
    }

    private class RawTextMessage
    {
        public string text { get; set; } = string.Empty;
    }
    */
}