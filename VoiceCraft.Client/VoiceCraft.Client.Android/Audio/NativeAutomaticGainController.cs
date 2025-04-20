using Android.Media.Audiofx;
using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAutomaticGainController : IAutomaticGainController
    {
        public bool IsNative => true;

        private AutomaticGainControl? _gainController;
        private bool _disposed;

        ~NativeAutomaticGainController()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new InvalidOperationException(Locales.Locales.Audio_AGC_InitFailed);
            
            CleanupGainController();
            _gainController = AutomaticGainControl.Create(audioRecorder.SessionId);
            
            if(_gainController == null)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_InitFailed);
        }

        public void Process(byte[] buffer) => Process(buffer.AsSpan());

        public void Process(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupGainController()
        {
            if (_gainController == null) return;
            _gainController.Dispose();
            _gainController = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(NativeAutomaticGainController).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_gainController == null)
                throw new InvalidOperationException(Locales.Locales.Audio_AGC_Init);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupGainController();
            _disposed = true;
        }
    }
}