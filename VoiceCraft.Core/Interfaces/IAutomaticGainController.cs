using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAutomaticGainController : IDisposable
    {
        bool IsNative { get; }
        
        void Initialize(IAudioRecorder recorder);
        
        void Process(byte[] buffer);
        
        void Process(Span<byte> buffer);
    }
}