using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class VoiceCraftEntity : INetSerializable
    {
        public event Action<string, VoiceCraftEntity>? OnNameUpdated;
        public event Action<EntityData, VoiceCraftEntity>? OnDataUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectAdded;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectUpdated;
        public event Action<IAudioEffect, VoiceCraftEntity>? OnEffectRemoved;
        public event Action<byte[], VoiceCraftEntity>? OnAudioReceived;

        private string _name = string.Empty;
        private EntityData _entityData;

        public int NetworkId { get; }
        public string WorldId { get; set; } = string.Empty;
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
        public EntityData EntityData
        {
            get => _entityData;
            set
            {
                if (value.Equals(_entityData)) return;
                _entityData = value;
                OnDataUpdated?.Invoke(_entityData, this);
            }
        }

        public IAudioEffect?[] Effects { get; } = new IAudioEffect[64]; //64 bit bitmask.
        //Modifiers for modifying data for later?

        protected VoiceCraftEntity(int networkId)
        {
            NetworkId = networkId;
        }

        public bool AddEffect(IAudioEffect effect)
        {
            for (var i = 0; i < Effects.Length; i++)
            {
                if (Effects[i] == null) continue;
                Effects[i] = effect;
                effect.OnEffectUpdated += EffectUpdated;
                OnEffectAdded?.Invoke(effect, this);
                return true;
            }

            return false;
        }

        public bool RemoveEffect(int effectId)
        {
            if (effectId < 0 || effectId >= Effects.Length) return false;
            var effect = Effects[effectId];
            if (effect == null) return false;
            Effects[effectId] = null;
            effect.OnEffectUpdated -= EffectUpdated;
            OnEffectRemoved?.Invoke(effect, this);
            return true;
        }

        public virtual void Write(byte[] buffer) => OnAudioReceived?.Invoke(buffer, this);

        public virtual int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(_name);
            writer.Put(EntityData);
        }

        public virtual void Deserialize(NetDataReader reader)
        {
            //Will trigger updates.
            Name = reader.GetString();
            
            var entityData = new EntityData();
            entityData.Deserialize(reader);
            EntityData = entityData;
        }
        
        private void EffectUpdated(IAudioEffect effect) => OnEffectUpdated?.Invoke(effect, this);
    }

    public struct EntityData : INetSerializable, IEquatable<EntityData>
    {
        public ulong Bitmask;
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Bitmask);
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
            Bitmask = reader.GetULong();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public bool Equals(EntityData other)
        {
            return Bitmask == other.Bitmask && Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Bitmask, Position, Rotation);
        }
    }
}