using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity : IResettable
    {
        private readonly Dictionary<int, VoiceCraftEntity> _visibleEntities = new Dictionary<int, VoiceCraftEntity>();

        //Privates
        private bool _muted;
        private bool _deafened;
        private float _loudness;
        private string _worldId = string.Empty;
        private string _name = "New Entity";
        private uint _listenBitmask = uint.MaxValue;
        private uint _talkBitmask = uint.MaxValue;
        private uint _effectBitmask = uint.MaxValue;
        private Vector3 _position;
        private Vector2 _rotation;

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(int id, VoiceCraftWorld world)
        {
            Id = id;
            World = world;
        }

        //Properties
        public virtual int Id { get; }
        public VoiceCraftWorld World { get; }
        public float Loudness => IsSpeaking ? _loudness : 0f;
        public bool IsSpeaking => (DateTime.UtcNow - LastSpoke).TotalMilliseconds < Constants.SilenceThresholdMs;
        public DateTime LastSpoke { get; private set; } = DateTime.MinValue;
        public bool Destroyed { get; private set; }

        public virtual void Reset()
        {
            Destroy();
        }

        //Entity events.
        public event Action<string, VoiceCraftEntity>? OnWorldIdUpdated;
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<bool, VoiceCraftEntity>? OnMuteUpdated;
        public event Action<bool, VoiceCraftEntity>? OnDeafenUpdated;
        public event Action<uint, VoiceCraftEntity>? OnTalkBitmaskUpdated;
        public event Action<uint, VoiceCraftEntity>? OnListenBitmaskUpdated;
        public event Action<uint, VoiceCraftEntity>? OnEffectBitmaskUpdated;
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Vector2, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityAdded;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityRemoved;
        public event Action<byte[], uint, float, VoiceCraftEntity>? OnAudioReceived;
        public event Action<VoiceCraftEntity>? OnDestroyed;

        public void AddVisibleEntity(VoiceCraftEntity entity)
        {
            if (!_visibleEntities.TryAdd(entity.Id, entity)) return;
            OnVisibleEntityAdded?.Invoke(entity, this);
        }

        public void RemoveVisibleEntity(VoiceCraftEntity entity)
        {
            if (!_visibleEntities.Remove(entity.Id)) return;
            OnVisibleEntityRemoved?.Invoke(entity, this);
        }

        public void TrimVisibleDeadEntities()
        {
            foreach (var entity in _visibleEntities.Where(entity => entity.Value.Destroyed).ToArray())
            {
                _visibleEntities.Remove(entity.Key);
            }
        }

        public virtual void ReceiveAudio(byte[] buffer, uint timestamp, float frameLoudness)
        {
            _loudness = frameLoudness;
            LastSpoke = DateTime.UtcNow;
            OnAudioReceived?.Invoke(buffer, timestamp, frameLoudness, this);
        }

        public virtual void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;
            OnDestroyed?.Invoke(this);

            //Deregister all events.
            OnWorldIdUpdated = null;
            OnNameUpdated = null;
            OnMuteUpdated = null;
            OnDeafenUpdated = null;
            OnTalkBitmaskUpdated = null;
            OnListenBitmaskUpdated = null;
            OnEffectBitmaskUpdated = null;
            OnPositionUpdated = null;
            OnRotationUpdated = null;
            OnVisibleEntityAdded = null;
            OnVisibleEntityRemoved = null;
            OnAudioReceived = null;
            OnDestroyed = null;
        }

        #region Updatable Properties

        public IEnumerable<VoiceCraftEntity> VisibleEntities => _visibleEntities.Values;

        public string WorldId
        {
            get => _worldId;
            set
            {
                if (_worldId == value) return;
                if (value.Length > Constants.MaxStringLength) throw new ArgumentOutOfRangeException();
                _worldId = value;
                OnWorldIdUpdated?.Invoke(_worldId, this);
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                if (value.Length > Constants.MaxStringLength) throw new ArgumentOutOfRangeException();
                _name = value;
                OnNameUpdated?.Invoke(_name, this);
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                if (_muted == value) return;
                _muted = value;
                OnMuteUpdated?.Invoke(_muted, this);
            }
        }

        public bool Deafened
        {
            get => _deafened;
            set
            {
                if (_deafened == value) return;
                _deafened = value;
                OnDeafenUpdated?.Invoke(_deafened, this);
            }
        }

        public uint TalkBitmask
        {
            get => _talkBitmask;
            set
            {
                if (_talkBitmask == value) return;
                _talkBitmask = value;
                OnListenBitmaskUpdated?.Invoke(_talkBitmask, this);
            }
        }

        public uint ListenBitmask
        {
            get => _listenBitmask;
            set
            {
                if (_listenBitmask == value) return;
                _listenBitmask = value;
                OnTalkBitmaskUpdated?.Invoke(_listenBitmask, this);
            }
        }

        public uint EffectBitmask
        {
            get => _effectBitmask;
            set
            {
                if (_effectBitmask == value) return;
                _effectBitmask = value;
                OnEffectBitmaskUpdated?.Invoke(_effectBitmask, this);
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

        public Vector2 Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value) return;
                _rotation = value;
                OnRotationUpdated?.Invoke(_rotation, this);
            }
        }

        #endregion
    }
}