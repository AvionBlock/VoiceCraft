using System;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IPreprocessor : IDisposable
    {
        bool IsNative { get; }

        bool IsGainControllerAvailable { get; }

        bool IsNoiseSuppressorAvailable { get; }

        bool IsVoiceActivityDetectionAvailable { get; }

        bool GainControllerEnabled { get; set; }

        bool NoiseSuppressorEnabled { get; set; }

        bool VoiceActivityDetectionEnabled { get; set; }

        void Init(IAudioRecorder recorder);

        bool Process(Span<byte> buffer);

        bool Process(byte[] buffer);
    }
}