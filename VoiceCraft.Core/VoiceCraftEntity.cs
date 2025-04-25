using System;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity : INetSerializable
    {
        //Entity events.
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnTalkBitmaskUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnListenBitmaskUpdated;
        public event Action<int, VoiceCraftEntity>? OnMinRangeUpdated;
        public event Action<int, VoiceCraftEntity>? OnMaxRangeUpdated;
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Quaternion, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<PropertyKey, object?, VoiceCraftEntity>? OnPropertySet;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityAdded;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityRemoved;
        public event Action<byte[], uint, float, VoiceCraftEntity>? OnAudioReceived;
        public event Action<VoiceCraftEntity>? OnDestroyed;
        
        //Privates
        private readonly List<VoiceCraftEntity> _visibleEntities = new List<VoiceCraftEntity>();
        private string _name = "New Entity";
        private string _worldId = string.Empty;
        private ulong _talkBitmask = 1;
        private ulong _listenBitmask = 1;
        private int _minRange;
        private int _maxRange;
        private Vector3 _position;
        private Quaternion _rotation;
        private readonly Dictionary<PropertyKey, object> _properties = new Dictionary<PropertyKey, object>();

        //Properties
        public int Id { get; }
        public float Loudness { get; private set; }
        public bool IsSpeaking => (DateTime.UtcNow - LastSpoke).TotalMilliseconds < Constants.SilenceThresholdMs;
        public DateTime LastSpoke { get; private set; } = DateTime.MinValue;
        public IEnumerable<VoiceCraftEntity> VisibleEntities => _visibleEntities;
        public IEnumerable<KeyValuePair<PropertyKey, object>> Properties => _properties;
        public bool Destroyed { get; private set; }
        
        #region Updatable Properties
        
        public string WorldId
        {
            get => _worldId;
            set
            {
                if (_worldId == value) return;
                if (value.Length > Constants.MaxStringLength) throw new ArgumentOutOfRangeException();
                _worldId = value;
                OnNameUpdated?.Invoke(_worldId, this);
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
        
        public ulong TalkBitmask
        {
            get => _talkBitmask;
            set
            {
                if (_talkBitmask == value) return;
                _talkBitmask = value;
                OnListenBitmaskUpdated?.Invoke(_talkBitmask, this);
            }
        }

        public ulong ListenBitmask
        {
            get => _listenBitmask;
            set
            {
                if (_listenBitmask == value) return;
                _listenBitmask = value;
                OnTalkBitmaskUpdated?.Invoke(_listenBitmask, this);
            }
        }

        public int MinRange
        {
            get => _minRange;
            set
            {
                if (_minRange == value) return;
                _minRange = value;
                OnMinRangeUpdated?.Invoke(_minRange, this);
            }
        }

        public int MaxRange
        {
            get => _maxRange;
            set
            {
                if (_maxRange == value) return;
                _maxRange = value;
                OnMaxRangeUpdated?.Invoke(_maxRange, this);
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
        
        #endregion

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(int id)
        {
            Id = id;
        }
        
        public void SetProperty(PropertyKey key, object? value)
        {
            if (key == PropertyKey.Unknown)
                throw new ArgumentOutOfRangeException(nameof(key));

            switch (value)
            {
                case byte _:
                case int _:
                case uint _:
                case float _:
                case null:
                    break;
                default:
                    throw new ArgumentException("Invalid argument type!", nameof(value));
            }

            //Null values aren't stored.
            if (value == null)
            {
                if(_properties.Remove(key))
                    OnPropertySet?.Invoke(key, null, this);
                return;
            }
            
            if(!_properties.TryAdd(key, value)) 
                _properties[key] = value;
            OnPropertySet?.Invoke(key, value, this);
        }
        
        public void ClearProperties()
        {
            _properties.Clear();
        }
        
        public void AddVisibleEntity(VoiceCraftEntity entity)
        {
            if(_visibleEntities.Contains(entity)) return;
            _visibleEntities.Add(entity);
            OnVisibleEntityAdded?.Invoke(entity, this);
        }

        public void RemoveVisibleEntity(VoiceCraftEntity entity)
        {
            if(!_visibleEntities.Remove(entity)) return;
            OnVisibleEntityRemoved?.Invoke(entity, this);
        }

        public void TrimVisibleDeadEntities()
        {
            _visibleEntities.RemoveAll(x => x.Destroyed);
        }

        public virtual void ReceiveAudio(byte[] buffer, uint timestamp, float frameLoudness)
        {
            Loudness = frameLoudness;
            LastSpoke = DateTime.UtcNow;
            OnAudioReceived?.Invoke(buffer, timestamp, frameLoudness, this);
        }

        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            var bitmask = TalkBitmask & entity.ListenBitmask;
            if (bitmask == 0) return false;
            if ((bitmask & 1ul) == 0) return true; //Proximity checking disabled.

            var maxRange = Math.Max(_maxRange, entity.MaxRange);
            var distance = Vector3.Distance(Position, entity.Position);
            return distance <= maxRange;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Name, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            var name = reader.GetString(Constants.MaxStringLength);
            Name = name;
        }

        public virtual void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;
            OnDestroyed?.Invoke(this);
            
            //Deregister all events.
            OnNameUpdated = null;
            OnTalkBitmaskUpdated = null;
            OnListenBitmaskUpdated = null;
            OnMinRangeUpdated = null;
            OnMaxRangeUpdated = null;
            OnPositionUpdated = null;
            OnRotationUpdated = null;
            OnPropertySet = null;
            OnVisibleEntityAdded = null;
            OnVisibleEntityRemoved = null;
            OnAudioReceived = null;
            OnDestroyed = null;
        }
    }
}