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
        private float _loudness;

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
            get;
            set
            {
                if (field == value) return;
                if (value.Length > Constants.MaxStringLength) throw new ArgumentOutOfRangeException();
                field = value;
                OnWorldIdUpdated?.Invoke(field, this);
            }
        } = string.Empty;

        public string Name
        {
            get;
            set
            {
                if (field == value) return;
                if (value.Length > Constants.MaxStringLength) throw new ArgumentOutOfRangeException();
                field = value;
                OnNameUpdated?.Invoke(field, this);
            }
        } = "New Entity";

        public bool Muted
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnMuteUpdated?.Invoke(field, this);
            }
        }

        public bool Deafened
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnDeafenUpdated?.Invoke(field, this);
            }
        }

        public ushort TalkBitmask
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnTalkBitmaskUpdated?.Invoke(field, this);
            }
        } = ushort.MaxValue;

        public ushort ListenBitmask
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnListenBitmaskUpdated?.Invoke(field, this);
            }
        } = ushort.MaxValue;

        public ushort EffectBitmask
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnEffectBitmaskUpdated?.Invoke(field, this);
            }
        } = ushort.MaxValue;

        public Vector3 Position
        {
            get;
            set
            {
                value = Sanitize(value);
                if (field == value) return;
                field = value;
                OnPositionUpdated?.Invoke(field, this);
            }
        }

        public Vector2 Rotation
        {
            get;
            set
            {
                value = Sanitize(value);
                if (field == value) return;
                field = value;
                OnRotationUpdated?.Invoke(field, this);
            }
        }

        public float CaveFactor
        {
            get;
            set
            {
                value = ClampFinite(value, 0f, 1f);
                if (Math.Abs(field - value) < Constants.FloatingPointTolerance) return;
                field = value;
                OnCaveFactorUpdated?.Invoke(field, this);
            }
        }

        public float MuffleFactor
        {
            get;
            set
            {
                value = ClampFinite(value, 0f, 1f);
                if (Math.Abs(field - value) < Constants.FloatingPointTolerance) return;
                field = value;
                OnMuffleFactorUpdated?.Invoke(field, this);
            }
        }

        #endregion

        private static Vector3 Sanitize(Vector3 value)
        {
            return new Vector3(
                Sanitize(value.X),
                Sanitize(value.Y),
                Sanitize(value.Z));
        }

        private static Vector2 Sanitize(Vector2 value)
        {
            return new Vector2(
                Sanitize(value.X),
                Sanitize(value.Y));
        }

        private static float Sanitize(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        private static float ClampFinite(float value, float min, float max)
        {
            return float.IsFinite(value) ? Math.Clamp(value, min, max) : min;
        }
    }
}
