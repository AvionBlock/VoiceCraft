using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public abstract class AudioSourceComponent : IUpdateable<AudioSourceComponent>
    {
        private readonly World _world;
        private readonly List<IAudioEffect> _audioEffects = new List<IAudioEffect>();
        private string _name = string.Empty;
        public event Action<AudioSourceComponent>? OnUpdate;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnUpdate?.Invoke(this);
            }
        }

        public uint MinRange { get; set; } = 0;
        public uint MaxRange { get; set; } = 30;
        public IAudioInput? AudioInput { get; set; }
        public IAudioEffect[] AudioEffects => _audioEffects.ToArray();

        protected AudioSourceComponent(World world)
        {
            _world = world;
        }

        public void AddAudioEffect(IAudioEffect audioEffect)
        {
            _audioEffects.Add(audioEffect);
        }

        public void RemoveAudioEffect(int index)
        {
            _audioEffects.RemoveAt(index);
        }
    }
}