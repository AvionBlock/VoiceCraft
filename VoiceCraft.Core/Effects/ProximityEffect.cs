using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        private ulong _bitmask;
        private uint _minRange;
        private uint _maxRange;

        public EffectType EffectType => EffectType.Proximity;

        public event Action<IAudioEffect>? OnEffectUpdated;

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value) return;
                _bitmask = value;
                OnEffectUpdated?.Invoke(this);
            }
        }

        public uint MinRange
        {
            get => _minRange;
            set
            {
                if (_minRange == value) return;
                _minRange = value;
                OnEffectUpdated?.Invoke(this);
            }
        }

        public uint MaxRange
        {
            get => _maxRange;
            set
            {
                if (_maxRange == value) return;
                _maxRange = value;
                OnEffectUpdated?.Invoke(this);
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(_bitmask);
            writer.Put(_minRange);
            writer.Put(_maxRange);
        }

        public void Deserialize(NetDataReader reader)
        {
            _bitmask = reader.GetULong();
            _minRange = reader.GetUInt();
            _maxRange = reader.GetUInt();
            OnEffectUpdated?.Invoke(this);
        }

        public bool VisibleTo(VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity, ulong bitmask)
        {
            //TODO Implement this!
            return true;
        }
    }
}