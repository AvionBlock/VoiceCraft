using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity : INetSerializable
    {
        //Data updates.
        public event Action<Vector3, VoiceCraftEntity>? OnPositionUpdated;
        public event Action<Quaternion, VoiceCraftEntity>? OnRotationUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnTalkBitmaskUpdated;
        public event Action<ulong, VoiceCraftEntity>? OnListenBitmaskUpdated;
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        
        //Effect Updates.
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectAdded;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectRemoved;
        
        //Other Updates.
        public event Action<byte[], VoiceCraftEntity>? OnAudioReceived;
        
        //Privates
        private string _name = "New Entity";
        private string _worldId = string.Empty;
        private ulong _talkBitmask;
        private ulong _listenBitmask;
        private Vector3 _position;
        private Quaternion _rotation;
        private readonly ConcurrentDictionary<EffectType, IAudioEffect> _effects = new ConcurrentDictionary<EffectType, IAudioEffect>();

        //Properties
        public int NetworkId { get; }
        public IEnumerable<KeyValuePair<EffectType, IAudioEffect>> Effects => _effects;

        //Updatable Properties
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

        //Modifiers for modifying data for later?

        public VoiceCraftEntity(int networkId)
        {
            NetworkId = networkId;
        }

        public bool AddEffect(IAudioEffect effect)
        {
            if (!_effects.TryAdd(effect.EffectType, effect)) return false;
            OnEffectAdded?.Invoke(effect, this);
            return true;
        }

        public bool HasEffect<T>(EffectType effectType) where T : IAudioEffect
        {
            if (!_effects.TryGetValue(effectType, out var effect)) return false;
            return effect.GetType() == typeof(T);
        }

        public T GetEffect<T>(EffectType effectType) where T : IAudioEffect
        {
            _effects.TryGetValue(effectType, out var effect);
            if (effect is T effectObject) return effectObject;
            throw new Exception($"No effect of type {typeof(T).Name}");
        }

        public bool RemoveEffect(EffectType effectType)
        {
            if (!_effects.TryRemove(effectType, out var effect)) return false;
            OnEffectRemoved?.Invoke(effect, this);
            return true;
        }

        public virtual void Write(byte[] buffer)
        {
            OnAudioReceived?.Invoke(buffer, this);
        }

        public virtual int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            var combinedBitmask = TalkBitmask & entity.ListenBitmask;
            if (combinedBitmask == 0) return false;

            foreach (var effect in _effects)
            {
                if(!(effect.Value is IVisible visibleEffect)) continue;
                if (visibleEffect.VisibleTo(this, entity, combinedBitmask)) continue;
                return false;
            }

            foreach (var effect in entity.Effects)
            {
                if (!(effect.Value is IVisible visibleEffect)) continue;
                if (visibleEffect.VisibleTo(this, entity, combinedBitmask)) continue;
                return false;
            }
            
            return true;
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
    }
}