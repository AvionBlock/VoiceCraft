using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Components
{
    public class AudioSourceComponent
    {
        private readonly World _world;
        private readonly List<IAudioEffect> _audioEffects = new List<IAudioEffect>();

        public string Name { get; set; } = string.Empty;
        public uint MinRange { get; set; } = 0;
        public uint MaxRange { get; set; } = 30;
        public IAudioInput? AudioInput { get; set; }
        public IAudioEffect[] AudioEffects => _audioEffects.ToArray();

        public AudioSourceComponent(World world)
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