using System.Numerics;
using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct TransformComponent : IComponent
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }
}