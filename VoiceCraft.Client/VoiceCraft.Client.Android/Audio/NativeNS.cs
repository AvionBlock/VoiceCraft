using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeNS
    {
        public bool Enabled
        {
            get => _noiseSuppressor?.Enabled ?? _enabled; set
            {
                if (_noiseSuppressor != null)
                    _noiseSuppressor.SetEnabled(value);
                else
                    _enabled = value;
            }
        }
        private NoiseSuppressor? _noiseSuppressor;
        private bool _enabled = true;
        private bool _disposed;

        ~NativeNS()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();
            if (recorder is AudioRecorder audioRecorder && audioRecorder.SessionId != null)
            {
                if (_noiseSuppressor != null)
                {
                    _noiseSuppressor.Release();
                    _noiseSuppressor.Dispose();
                    _noiseSuppressor = null;
                }

                _noiseSuppressor = NoiseSuppressor.Create((int)audioRecorder.SessionId);
                _noiseSuppressor?.SetEnabled(_enabled); //Force setting of NS.
            }
            else
            {
                throw new Exception($"{nameof(recorder)} must be type of {typeof(AudioRecorder)}.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
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
            if (_disposed)
                throw new ObjectDisposedException(nameof(NativeAEC));
        }
    }
}
