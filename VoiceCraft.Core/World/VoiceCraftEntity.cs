using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftEntity(int id)
    {
        private readonly ConcurrentDictionary<int, VoiceCraftEntity> _visibleEntities = new();
        private float _caveFactor;
        private bool _deafened;
        private ushort _effectBitmask = ushort.MaxValue;
        private ushort _listenBitmask = ushort.MaxValue;
        private float _loudness;
        private float _muffleFactor;
        private bool _muted;
        private string _name = "New Entity";
        private Vector3 _position;
        private Vector2 _rotation;
        private ushort _talkBitmask = ushort.MaxValue;
        private string _worldId = string.Empty;

        //Properties
        public int Id { get; } = id;
        public float Loudness => IsSpeaking ? _loudness : 0f;
        public bool IsSpeaking => (DateTime.UtcNow - LastSpoke).TotalMilliseconds < Constants.SilenceThresholdMs;
        public DateTime LastSpoke { get; private set; } = DateTime.MinValue;
        public bool Destroyed { get; private set; }

        public virtual void Reset()
        {
            Destroy();
        }

        //Entity events.
        //Properties
        public event Action<string, VoiceCraftEntity>? OnWorldIdUpdated;
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<bool, VoiceCraftEntity>? OnMuteUpdated;
        public event Action<bool, VoiceCraftEntity>? OnDeafenUpdated;
        public event Action<ushort, VoiceCraftEntity>? OnTalkBitmaskUpdated;
        public event Action<ushort, VoiceCraftEntity>? OnListenBitmaskUpdated;
        public event Action<ushort, VoiceCraftEntity>? OnEffectBitmaskUpdated;
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Vector2, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<float, VoiceCraftEntity>? OnCaveFactorUpdated;
        public event Action<float, VoiceCraftEntity>? OnMuffleFactorUpdated;

        //Others
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityAdded;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityRemoved;
        public event Action<byte[], ushort, float, VoiceCraftEntity>? OnAudioReceived;
        public event Action<VoiceCraftEntity>? OnDestroyed;

        public void AddVisibleEntity(VoiceCraftEntity entity)
        {
            if (entity == this) return;
            if (!_visibleEntities.TryAdd(entity.Id, entity)) return;
            OnVisibleEntityAdded?.Invoke(entity, this);
        }

        public void RemoveVisibleEntity(VoiceCraftEntity entity)
        {
            if (entity == this) return;
            if (!_visibleEntities.Remove(entity.Id, out _)) return;
            OnVisibleEntityRemoved?.Invoke(entity, this);
        }

        public void TrimDeadEntities()
        {
            List<int>? keysToRemove = null;
            foreach (var entity in _visibleEntities.Where(entity => entity.Value.Destroyed))
                (keysToRemove ??= []).Add(entity.Key);

            if (keysToRemove == null) return;
            foreach (var key in keysToRemove)
                _visibleEntities.Remove(key, out _);
        }

        public virtual void ReceiveAudio(byte[] buffer, ushort timestamp, float frameLoudness)
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
            OnCaveFactorUpdated = null;
            OnMuffleFactorUpdated = null;
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

        public ushort TalkBitmask
        {
            get => _talkBitmask;
            set
            {
                if (_talkBitmask == value) return;
                _talkBitmask = value;
                OnTalkBitmaskUpdated?.Invoke(_talkBitmask, this);
            }
        }

        public ushort ListenBitmask
        {
            get => _listenBitmask;
            set
            {
                if (_listenBitmask == value) return;
                _listenBitmask = value;
                OnListenBitmaskUpdated?.Invoke(_listenBitmask, this);
            }
        }

        public ushort EffectBitmask
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

        public float CaveFactor
        {
            get => _caveFactor;
            set
            {
                if (Math.Abs(_caveFactor - value) < Constants.FloatingPointTolerance) return;
                _caveFactor = Math.Clamp(value, 0f, 1f);
                OnCaveFactorUpdated?.Invoke(_caveFactor, this);
            }
        }

        public float MuffleFactor
        {
            get => _muffleFactor;
            set
            {
                if (Math.Abs(_muffleFactor - value) < Constants.FloatingPointTolerance) return;
                _muffleFactor = Math.Clamp(value, 0f, 1f);
                OnMuffleFactorUpdated?.Invoke(_muffleFactor, this);
            }
        }

        #endregion
    }
}