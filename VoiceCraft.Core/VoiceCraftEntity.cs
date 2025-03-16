using System;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity
    {
        private static readonly Dictionary<int, VoiceCraftEntity> Entities = new Dictionary<int, VoiceCraftEntity>();
        
        public event Action<ulong, VoiceCraftEntity>? OnBitmaskUpdated;
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<EntityData, VoiceCraftEntity>? OnDataUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectAdded;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectRemoved;
        public event Action<byte[], VoiceCraftEntity>? OnAudioReceived;

        private ulong _bitmask;
        private string _name = string.Empty;
        private EntityData _entityData;
        private bool _destroyed;

        public int NetworkId { get; }
        public string WorldId { get; set; } = string.Empty;
        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if(_destroyed || _bitmask == value) return;
                _bitmask = value;
                OnBitmaskUpdated?.Invoke(_bitmask, this);
            }
        }
        public string Name
        {
            get => _name;
            set
            {
                if (_destroyed || _name != value) return;
                _name = value;
                OnNameUpdated?.Invoke(_name, this);
            }
        }
        public EntityData EntityData
        {
            get => _entityData;
            set
            {
                if (_destroyed || value.Equals(_entityData)) return;
                _entityData = value;
                OnDataUpdated?.Invoke(_entityData, this);
            }
        }

        public Dictionary<EffectType, IAudioEffect> Effects { get; } = new Dictionary<EffectType, IAudioEffect>();
        //Modifiers for modifying data for later?

        protected VoiceCraftEntity(int networkId)
        {
            if(Entities.ContainsKey(networkId))
                throw new InvalidOperationException($"An entity with the network ID {networkId} has already been added!");
            Entities.Add(networkId, this);
            NetworkId = networkId;
        }

        public bool AddEffect(IAudioEffect effect)
        {
            if (_destroyed || !Effects.TryAdd(effect.EffectType, effect)) return false;
            effect.OnEffectUpdated += EffectUpdated;
            OnEffectAdded?.Invoke(effect, this);
            return true;

        }

        public bool RemoveEffect(EffectType effectType)
        {
            if(_destroyed || !Effects.Remove(effectType, out var effect)) return false;
            effect.OnEffectUpdated -= EffectUpdated;
            OnEffectRemoved?.Invoke(effect, this);
            return true;
        }

        public virtual void Write(byte[] buffer)
        {
            if(_destroyed) return;
            OnAudioReceived?.Invoke(buffer, this);
        }

        public virtual int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        
        public bool VisibleTo(VoiceCraftEntity entity)
        {
            if (string.IsNullOrWhiteSpace(WorldId) || string.IsNullOrWhiteSpace(entity.WorldId) || WorldId != entity.WorldId) return false;
            var combinedBitmask =  Bitmask | entity.Bitmask;
            if (combinedBitmask == 0) return false;
            
            foreach (var effect in Effects)
            {
                if (!(effect.Value is IVisible visible)) continue;
                if (!visible.VisibleTo(this, entity, combinedBitmask)) return false;
            }

            return true;
        }

        public void Destroy()
        {
            Entities.Remove(NetworkId);
            _destroyed = true;
        }
        
        private void EffectUpdated(IAudioEffect effect) => OnEffectUpdated?.Invoke(effect, this);

        public static VoiceCraftEntity? GetEntityFromNetworkId(int networkId)
        {
            Entities.TryGetValue(networkId, out var entity);
            return entity;
        }

        public static bool Destroy(int networkId)
        {
            var entity = GetEntityFromNetworkId(networkId);
            if(entity == null) return false;
            entity.Destroy();
            return true;
        }
    }

    public struct EntityData : INetSerializable, IEquatable<EntityData>
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(NetDataWriter writer)
        {
            //Position
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);

            //Rotation
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
        }

        public void Deserialize(NetDataReader reader)
        {
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public bool Equals(EntityData other)
        {
            return Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Rotation);
        }
    }
}