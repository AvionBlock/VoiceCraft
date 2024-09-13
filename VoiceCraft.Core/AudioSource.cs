using System.Numerics;

namespace VoiceCraft.Core
{
    public abstract class AudioSource
    {
        public float Gain = 1.0f;
        public Vector3 Position = new Vector3();
        public Vector3 Velocity = new Vector3();
        public uint Bitmask = 0;
    }
}
