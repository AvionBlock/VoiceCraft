﻿namespace VoiceCraft.Core.Packets.MCWSS
{
    public class PlayerTravelledEvent
    {
        public bool isUnderwater { get; set; }
        public float metersTravelled { get; set; }
        public int newBiome { get; set; }
        public Player player { get; set; } = new Player();
        public int travelMethod { get; set; }
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
        public float yRot {  get; set; }
    }

    public struct Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}
