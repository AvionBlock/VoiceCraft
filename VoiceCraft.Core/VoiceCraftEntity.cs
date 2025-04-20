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
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Quaternion, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<string, object, VoiceCraftEntity>? OnPropertySet;
        public event Action<string, object, VoiceCraftEntity>? OnPropertyRemoved;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityAdded;
        public event Action<VoiceCraftEntity, VoiceCraftEntity>? OnVisibleEntityRemoved;
        public event Action<byte[], uint, VoiceCraftEntity>? OnAudioReceived;
        public event Action<VoiceCraftEntity>? OnDestroyed;
        
        //Privates
        private readonly List<VoiceCraftEntity> _visibleEntities = new List<VoiceCraftEntity>();
        private string _name = "New Entity";
        private string _worldId = string.Empty;
        private ulong _talkBitmask = 1;
        private ulong _listenBitmask = 1;
        private Vector3 _position;
        private Quaternion _rotation;
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        //Properties
        public byte Id { get; }
        public bool IsSpeaking => (DateTime.UtcNow - LastSpoke).TotalMilliseconds < Constants.SilenceThresholdMs;
        public bool Destroyed { get; private set; }
        public DateTime LastSpoke { get; private set; } = DateTime.MinValue;
        public IEnumerable<VoiceCraftEntity> VisibleEntities => _visibleEntities;
        
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
        
        public IEnumerable<KeyValuePair<string, object>> Properties => _properties;
        #endregion

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(byte id)
        {
            Id = id;
        }
        
        public void SetProperty(string name, object value)
        {
            if (name.Length > Constants.MaxStringLength)
                throw new ArgumentOutOfRangeException(nameof(name));

            switch (value)
            {
                case byte _:
                case int _:
                case uint _:
                case float _:
                    break;
                default:
                    throw new ArgumentException("Invalid argument type!", nameof(value));
            }
            
            if(!_properties.TryAdd(name, value)) 
                _properties[name] = value;
            OnPropertySet?.Invoke(name, value, this);
        }

        public void RemoveProperty(string name)
        {
            if(_properties.Remove(name, out var value))
                OnPropertyRemoved?.Invoke(name, value, this);
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

        public void ReceiveAudio(byte[] buffer, uint timestamp)
        {
            LastSpoke = DateTime.UtcNow;
            OnAudioReceived?.Invoke(buffer, timestamp, this);
        }

        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            //TODO, IMPLEMENT PROXIMITY CHECKING LOGIC!
            return (TalkBitmask & entity.ListenBitmask) != 0; //Check talk and listen bitmask.
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Name);
            writer.Put(TalkBitmask);
            writer.Put(ListenBitmask);
        }

        public void Deserialize(NetDataReader reader)
        {
            var name = reader.GetString();
            var talkBitmask = reader.GetULong();
            var listenBitmask = reader.GetULong();
            
            Name = name;
            TalkBitmask = talkBitmask;
            ListenBitmask = listenBitmask;
        }

        public void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;
            OnDestroyed?.Invoke(this);
            
            //Deregister all events.
            OnNameUpdated = null;
            OnTalkBitmaskUpdated = null;
            OnListenBitmaskUpdated = null;
            OnPositionUpdated = null;
            OnRotationUpdated = null;
            OnPropertySet = null;
            OnPropertyRemoved = null;
            OnVisibleEntityAdded = null;
            OnVisibleEntityRemoved = null;
            OnAudioReceived = null;
            OnDestroyed = null;
        }
    }
}