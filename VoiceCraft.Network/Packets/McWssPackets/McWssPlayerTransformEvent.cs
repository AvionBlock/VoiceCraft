namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class McWssPlayerTransformEvent : McWssPacket<McWssPlayerTransformEvent.McWssPlayerTransformEventBody>
    {
        public override McWssPlayerTransformEventBody body { get; set; } = new();

        //Resharper disable all
        public class McWssPlayerTransformEventBody
        {
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
}