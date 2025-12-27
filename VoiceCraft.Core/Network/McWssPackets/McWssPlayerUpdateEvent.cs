namespace VoiceCraft.Core.Network.McWssPackets
{
    //This is packet is for custom MCBE injected clients.
    public class McWssPlayerUpdateEvent: McWssPacket<McWssPlayerUpdateEvent.McWssPlayerUpdateEventBody>
    {
        public override McWssPlayerUpdateEventBody body { get; set; } = new McWssPlayerUpdateEventBody();

        //Resharper disable all
        public class McWssPlayerUpdateEventBody
        {
          public string playerName { get; set; } = string.Empty;
          public string worldId  { get; set; } = string.Empty;
          public Position position { get; set; }
          public Rotation rotation { get; set; }
          public float caveFactor { get; set; }
          public float mufflefactor { get; set; }
        }

        public struct Position
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public struct Rotation
        {
          public float x { get; set; }
          public float y { get; set; }
        }
        //Resharper enable all
    }
}