using System.Numerics;

namespace VoiceCraft.Server.Components
{
    public class TransformComponent
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformComponent()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TransformComponent(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }
    }
}