using System;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftServerClient
    {
        public event Action<byte[]>? OnAudioReceived;
        public event Action? OnDisconnected;
    }
}