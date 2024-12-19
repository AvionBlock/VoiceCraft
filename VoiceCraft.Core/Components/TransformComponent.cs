using System;
using System.Numerics;
using Arch.Core;

namespace VoiceCraft.Core.Components
{
    public class TransformComponent
    {
        protected readonly World World;
        public event Action<TransformComponent>? OnUpdated;
        
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        public Vector3 Position
        {
            get => _position;
            set
            {
                if (value == _position) return;
                _position = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if (value == _rotation) return;
                _rotation = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                if (value == _scale) return;
                _scale = value;
                OnUpdated?.Invoke(this);
            }
        }

        public TransformComponent(World world)
        {
            World = world;
        }
    }
}