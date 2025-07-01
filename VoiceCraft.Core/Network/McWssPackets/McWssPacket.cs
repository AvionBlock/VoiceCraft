namespace VoiceCraft.Core.Network.McWssPackets
{
    public abstract class McWssPacket<T> where T : notnull
    {
        //Resharper disable All
        public McWssHeader header { get; set; } = new McWssHeader();
        public abstract T body { get; set; }
        
        public class McWssHeader
        {
            public string requestId { get; set; } = string.Empty;
            public string messagePurpose { get; set; } = string.Empty;
            public string eventName { get; set; } = string.Empty;
            public int version { get; set; } = 1;
        }
        //Resharper enable All
    }
}