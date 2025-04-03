using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity : INetSerializable
    {
        //Entity events.
        public event Action<VoiceCraftNetworkEntity, VoiceCraftEntity>? OnVisibleEntityAdded;
        public event Action<VoiceCraftNetworkEntity, VoiceCraftEntity>? OnVisibleEntityRemoved;
        public event Action<byte[], uint, bool, VoiceCraftEntity>? OnAudioReceived;
        public event Action<VoiceCraftEntity>? OnDestroyed;
        
        #region Updatable Property Events
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnTalkBitmaskUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnListenBitmaskUpdated;
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Quaternion, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<string, int, VoiceCraftEntity>? OnIntPropertySet; //TOO MANY EVENTS!
        public event Action<string, bool, VoiceCraftEntity>? OnBoolPropertySet;
        public event Action<string, float, VoiceCraftEntity>? OnFloatPropertySet;
        public event Action<string, int, VoiceCraftEntity>? OnIntPropertyRemoved;
        public event Action<string, bool, VoiceCraftEntity>? OnBoolPropertyRemoved;
        public event Action<string, float, VoiceCraftEntity>? OnFloatPropertyRemoved;
        #endregion
        
        //Privates
        private readonly List<VoiceCraftNetworkEntity> _visibleEntities = new List<VoiceCraftNetworkEntity>();
        private readonly Dictionary<string, int> _intProperties = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _boolProperties = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> _floatProperties = new Dictionary<string, float>();
        [StringLength(Constants.MaxStringLength)]
        private string _name = "New Entity";
        [StringLength(Constants.MaxStringLength)]
        private string _worldId = string.Empty;
        private ulong _talkBitmask = 1;
        private ulong _listenBitmask = 1;
        private Vector3 _position;
        private Quaternion _rotation;
        private bool _endTransmission = true;

        //Properties
        public int Id { get; }
        public bool IsSpeaking => (DateTime.UtcNow - LastSpoke).TotalMilliseconds < Constants.SilenceThresholdMs || !_endTransmission;
        public bool Destroyed { get; private set; }
        public DateTime LastSpoke { get; private set; } = DateTime.MinValue;
        public IEnumerable<KeyValuePair<string, int>> IntProperties => _intProperties;
        public IEnumerable<KeyValuePair<string, bool>> BoolProperties => _boolProperties;
        public IEnumerable<KeyValuePair<string, float>> FloatProperties => _floatProperties;
        public IEnumerable<VoiceCraftNetworkEntity> VisibleEntities => _visibleEntities;
        
        #region Updatable Properties
        public string WorldId
        {
            get => _worldId;
            set
            {
                if (_worldId == value) return;
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
        #endregion

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(int id)
        {
            Id = id;
        }

        #region Property Methods
        public void SetProperty(string key, int value)
        {
            if(key.Length > Constants.MaxStringLength)
                throw new ArgumentException($"Key must be less than {Constants.MaxStringLength} characters long!", nameof(key));
            
            if (_intProperties.TryAdd(key, value))
            {
                OnIntPropertySet?.Invoke(key, value, this);
                return;
            }
            
            if(_intProperties[key] == value) return;
            _intProperties[key] = value;
            OnIntPropertyRemoved?.Invoke(key, value, this);
        }
        
        public void SetProperty(string key, bool value)
        {
            if(key.Length > Constants.MaxStringLength)
                throw new ArgumentException($"Key must be less than {Constants.MaxStringLength} characters long!", nameof(key));
            
            if (_boolProperties.TryAdd(key, value))
            {
                OnBoolPropertySet?.Invoke(key, value, this);
                return;
            }
            
            if(_boolProperties[key] == value) return;
            _boolProperties[key] = value;
            OnBoolPropertyRemoved?.Invoke(key, value, this);
        }
        
        public void SetProperty(string key, float value)
        {
            if(key.Length > Constants.MaxStringLength)
                throw new ArgumentException($"Key must be less than {Constants.MaxStringLength} characters long!", nameof(key));
            
            if (_floatProperties.TryAdd(key, value))
            {
                OnFloatPropertySet?.Invoke(key, value, this);
                return;
            }
            
            if(Math.Abs(_floatProperties[key] - value) < Constants.FloatingPointTolerance) return;
            _floatProperties[key] = value;
            OnFloatPropertyRemoved?.Invoke(key, value, this);
        }

        public int? GetIntProperty(string key)
        {
            if (!_intProperties.TryGetValue(key, out var value))
                return null;
            return value;
        }
        
        public bool? GetBoolProperty(string key)
        {
            if (!_boolProperties.TryGetValue(key, out var value))
                return null;
            return value;
        }
        
        public float? GetFloatProperty(string key)
        {
            if (!_floatProperties.TryGetValue(key, out var value))
                return null;
            return value;
        }

        public bool RemoveIntProperty(string key)
        {
            return _intProperties.Remove(key);
        }

        public bool RemoveBoolProperty(string key)
        {
            return _boolProperties.Remove(key);
        }

        public bool RemoveFloatProperty(string key)
        {
            return _floatProperties.Remove(key);
        }
        #endregion

        #region Visible Entity Methods
        public void AddVisibleEntity(VoiceCraftNetworkEntity entity)
        {
            if(_visibleEntities.Contains(entity)) return;
            _visibleEntities.Add(entity);
            OnVisibleEntityAdded?.Invoke(entity, this);
        }

        public void RemoveVisibleEntity(VoiceCraftNetworkEntity entity)
        {
            if(!_visibleEntities.Remove(entity)) return;
            OnVisibleEntityRemoved?.Invoke(entity, this);
        }

        public void TrimVisibleDeadEntities()
        {
            _visibleEntities.RemoveAll(x => x.Destroyed);
        }
        #endregion

        public void ReceiveAudio(byte[] buffer, uint timestamp, bool endOfTransmission)
        {
            _endTransmission = endOfTransmission;
            LastSpoke = DateTime.UtcNow;
            OnAudioReceived?.Invoke(buffer, timestamp, endOfTransmission, this);
        }

        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            return (TalkBitmask & entity.ListenBitmask) != 0; //Check talk and listen bitmask.
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Name);
            writer.Put(TalkBitmask);
            writer.Put(ListenBitmask);
            
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);

            writer.Put(_intProperties.Count);
            foreach (var property in _intProperties)
            {
                writer.Put(property.Key);
                writer.Put(property.Value);
            }
            
            writer.Put(_boolProperties.Count);
            foreach (var property in _boolProperties)
            {
                writer.Put(property.Key);
                writer.Put(property.Value);
            }
            
            writer.Put(_floatProperties.Count);
            foreach (var property in _floatProperties)
            {
                writer.Put(property.Key);
                writer.Put(property.Value);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            var name = reader.GetString();
            var talkBitmask = reader.GetULong();
            var listenBitmask = reader.GetULong();
            var positionX = reader.GetFloat();
            var positionY = reader.GetFloat();
            var positionZ = reader.GetFloat();
            
            var rotationX = reader.GetFloat();
            var rotationY = reader.GetFloat();
            var rotationZ = reader.GetFloat();
            var rotationW = reader.GetFloat();
            
            Name = name;
            TalkBitmask = talkBitmask;
            ListenBitmask = listenBitmask;
            Position = new Vector3(positionX, positionY, positionZ);
            Rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);
            
            _intProperties.Clear();
            _boolProperties.Clear();
            _floatProperties.Clear();
            
            var intPropertiesCount = reader.GetInt();
            for (var i = 0; i < intPropertiesCount; i++)
            {
                var key = reader.GetString();
                var value = reader.GetInt();
                
                _intProperties.Add(key, value);
            }
            
            var boolPropertiesCount = reader.GetInt();
            for (var i = 0; i < boolPropertiesCount; i++)
            {
                var key = reader.GetString();
                var value = reader.GetBool();
                
                _boolProperties.Add(key, value);
            }
            
            var floatPropertiesCount = reader.GetInt();
            for (var i = 0; i < floatPropertiesCount; i++)
            {
                var key = reader.GetString();
                var value = reader.GetFloat();
                
                _floatProperties.Add(key, value);
            }
        }

        public void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;
            OnDestroyed?.Invoke(this);
        }
    }
}