using System;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IAutomaticGainController : IDisposable
    {
        bool IsNative { get; }
        
        void Init(IAudioRecorder recorder);
        
        void Process(byte[] buffer);
        
        void Process(Span<byte> buffer);
    }
}