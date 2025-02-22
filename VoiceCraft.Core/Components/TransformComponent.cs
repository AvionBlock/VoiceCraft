using System.Numerics;

namespace VoiceCraft.Core.Components
{
    public struct TransformComponent
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public TransformComponent(Vector3? position = null, Quaternion? rotation = null)
        {
            Position = position ?? Vector3.Zero;
            Rotation = rotation ?? Quaternion.Identity;
        }
    }
}