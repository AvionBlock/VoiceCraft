using Android.Media.Audiofx;
using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeEchoCanceler : IEchoCanceler
    {
        public bool IsNative => true;

        private AcousticEchoCanceler? _echoCanceler;
        private bool _disposed;

        ~NativeEchoCanceler()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder, IAudioPlayer player) //We don't need to have the audio player, but it's there for other compatibility reasons.
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_InitFailed);

            CleanupEchoCanceler();
            _echoCanceler = AcousticEchoCanceler.Create(audioRecorder.SessionId);
            
            if(_echoCanceler == null)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_InitFailed);
        }

        public void EchoPlayback(Span<byte> buffer, int count)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
        }

        public void EchoPlayback(byte[] buffer, int count) => EchoPlayback(buffer.AsSpan(), count);

        public void EchoCancel(Span<byte> buffer, int count)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
        }

        public void EchoCancel(byte[] buffer, int count) => EchoCancel(buffer.AsSpan(), count);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupEchoCanceler()
        {
            if (_echoCanceler == null) return;
            _echoCanceler.Dispose();
            _echoCanceler = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(NativeEchoCanceler).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_echoCanceler == null)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_Init);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupEchoCanceler();
            _disposed = true;
        }
    }
}