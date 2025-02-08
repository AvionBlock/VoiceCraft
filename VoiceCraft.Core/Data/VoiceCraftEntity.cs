using System;
namespace VoiceCraft.Core.Data
{
    public class VoiceCraftEntity
    {
        public Transform Transform { get; set; } = new Transform();
        public event Action<byte[]>? OnAudioReceived;
        public event Action? OnDisconnected;
    }
}