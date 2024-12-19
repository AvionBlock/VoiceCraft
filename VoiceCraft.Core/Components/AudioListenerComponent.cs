using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public abstract class AudioListenerComponent : IAudioInput
    {
        protected readonly World World;
        protected readonly List<IAudioEffect> Effects = new List<IAudioEffect>();
        public event Action<AudioListenerComponent>? OnUpdate;
        
        public IAudioEffect[] AudioEffects => Effects.ToArray();

        protected AudioListenerComponent(World world)
        {
            World = world;
        }

        public virtual void AddAudioEffect(IAudioEffect audioEffect)
        {
            Effects.Add(audioEffect);
        }

        public virtual void RemoveAudioEffect(int index)
        {
            Effects.RemoveAt(index);
        }
    }
}