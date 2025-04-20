using Android.Media.Audiofx;
using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeDenoiser : IDenoiser
    {
        public bool IsNative => true;

        private NoiseSuppressor? _denoiser;
        private bool _disposed;

        ~NativeDenoiser()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new InvalidOperationException(Locales.Locales.Audio_DN_InitFailed);
            
            CleanupDenoiser();
            _denoiser = NoiseSuppressor.Create(audioRecorder.SessionId);
            
            if(_denoiser == null)
                throw new InvalidOperationException(Locales.Locales.Audio_DN_InitFailed);
        }

        public void Denoise(byte[] buffer) => Denoise(buffer.AsSpan());

        public void Denoise(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupDenoiser()
        {
            if (_denoiser == null) return;
            _denoiser.Dispose();
            _denoiser = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(NativeDenoiser).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_denoiser == null)
                throw new InvalidOperationException(Locales.Locales.Audio_DN_Init);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupDenoiser();
            _disposed = true;
        }
    }
}