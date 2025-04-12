using Android.Media.Audiofx;
using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeDenoiser : IDenoiser
    {
        public bool IsNative => true;

        private NoiseSuppressor? _noiseSuppressor;
        private AudioRecorder? _recorder;
        private bool _initialized;
        private bool _disposed;

        ~NativeDenoiser()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new ArgumentException(Locales.Locales.Android_NativeDN_Exception_AndroidRecorder, nameof(recorder));
            if (_noiseSuppressor != null)
            {
                _noiseSuppressor.Release();
                _noiseSuppressor.Dispose();
                _noiseSuppressor = null;
            }

            _recorder = audioRecorder;
            _initialized = false;
        }

        public void Denoise(byte[] buffer) => Denoise(buffer.AsSpan());

        public void Denoise(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_initialized) return;
            if (_recorder?.SessionId == null) throw new InvalidOperationException(Locales.Locales.Android_NativeDN_Exception_Init);
            if (_noiseSuppressor != null)
            {
                _noiseSuppressor.Release();
                _noiseSuppressor.Dispose();
                _noiseSuppressor = null;
            }
            
            _noiseSuppressor = NoiseSuppressor.Create((int)_recorder.SessionId);
            _initialized = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_noiseSuppressor != null)
                {
                    _noiseSuppressor.Release();
                    _noiseSuppressor.Dispose();
                    _noiseSuppressor = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(NativeDenoiser));
        }
    }
}