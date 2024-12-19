using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public abstract class AudioSourceComponent : IAudioSource
    {
        protected readonly World World;
        protected readonly Entity Entity;
        protected string Name = string.Empty;
        protected uint MinRange = 0;
        protected uint MaxRange = 0;
        protected IAudioSource? AudioInput;
        public event Action<AudioSourceComponent>? OnUpdated;

        public string SourceName
        {
            get => Name;
            set
            {
                if (value == Name) return;
                Name = value;
                OnUpdated?.Invoke(this);
            }
        }

        public IAudioSource? SourceAudioInput
        {
            get => AudioInput;
            set
            {
                if(value == AudioInput) return;
                AudioInput = value;
                OnUpdated?.Invoke(this);
            }
        }

        public uint SourceMinRange
        {
            get => MinRange;
            set
            {
                if (value == MinRange) return;
                MinRange = value;
                OnUpdated?.Invoke(this);
            }
        }

        public uint SourceMaxRange
        {
            get => MaxRange;
            set
            {
                if (value == MaxRange) return;
                MaxRange = value;
                OnUpdated?.Invoke(this);
            }
        }

        protected AudioSourceComponent(World world, ref Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public virtual int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void GetTrackableEntities(List<Entity> entities)
        {
            if (entities.Contains(Entity)) return;
            entities.Add(Entity);
            AudioInput?.GetTrackableEntities(entities);
        }
    }
}