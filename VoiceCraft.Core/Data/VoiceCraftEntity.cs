using System;
namespace VoiceCraft.Core.Data
{
    public class VoiceCraftEntity
    {
        public Guid Id = Guid.NewGuid();
        public string Name = string.Empty;
        public string? EnvironmentId = null;
        public Transform Transform = new Transform();
        public event Action<byte[]>? OnAudioReceived;
        public event Action? OnDisconnected;
    }
}