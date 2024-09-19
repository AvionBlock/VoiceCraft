using System.Numerics;

namespace VoiceCraft.Core.Audio
{
    public abstract class AudioCapture
    {
        public Vector3 Position = new Vector3();
        public Vector3 Velocity = new Vector3();
        public uint Bitmask = 0;
    }
}
