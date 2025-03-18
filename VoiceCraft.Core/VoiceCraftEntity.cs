using System;
using System.Numerics;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity
    {
        //Data updates.
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Quaternion, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnBitmaskUpdated;
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        
        //Effect Updates.
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectAdded;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectRemoved;
        
        //Other Updates.
        public event Action<byte[], VoiceCraftEntity>? OnAudioReceived;
        
        //Privates
        private string _name = string.Empty;
        private string _worldId = string.Empty;
        private ulong _bitmask;
        private Vector3 _position;
        private Quaternion _rotation;

        //Properties
        public int NetworkId { get; }

        //Updatable Properties
        public string WorldId
        {
            get => _worldId;
            set
            {
                if (_worldId != value) return;
                _worldId = value;
                OnNameUpdated?.Invoke(_worldId, this);
            }
        }
        
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value) return;
                _name = value;
                OnNameUpdated?.Invoke(_name, this);
            }
        }
        
        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value) return;
                _bitmask = value;
                OnBitmaskUpdated?.Invoke(_bitmask, this);
            }
        }
        
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                OnPositionUpdated?.Invoke(_position, this);
            }
        }
        
        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value) return;
                _rotation = value;
                OnRotationUpdated?.Invoke(_rotation, this);
            }
        }

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(int networkId)
        {
            NetworkId = networkId;
        }

        public virtual void Write(byte[] buffer)
        {
            OnAudioReceived?.Invoke(buffer, this);
        }

        public virtual int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            var combinedBitmask = Bitmask & entity.Bitmask;
            return combinedBitmask != 0;
        }
    }
}