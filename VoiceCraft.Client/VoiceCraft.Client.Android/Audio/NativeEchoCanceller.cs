using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeEchoCanceller : IEchoCanceller
    {
        public bool Enabled { get => _echoCanceler?.Enabled ?? _enabled; set
            {
                if (_echoCanceler != null) _echoCanceler.SetEnabled(value);
                _enabled = value;
            }
        }
        private AcousticEchoCanceler? _echoCanceler;
        private bool _enabled = true;
        private bool _disposed;

        ~NativeEchoCanceller()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();
            if (recorder is AudioRecorder audioRecorder && audioRecorder.SessionId != null)
            {
                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }

                _echoCanceler = AcousticEchoCanceler.Create((int)audioRecorder.SessionId);
                _echoCanceler?.SetEnabled(_enabled); //Force setting of AEC.
            }
            else
            {
                throw new Exception($"{nameof(recorder)} must be type of {typeof(AudioRecorder)}.");
            }
        }

        public void EchoPlayback(byte[] buffer)
        {
            ThrowIfDisposed();
            return;
        }

        public void EchoPlayback(Span<byte> buffer)
        {
            ThrowIfDisposed();
            return;
        }

        public void EchoCancel(byte[] buffer)
        {
            ThrowIfDisposed();
            return;
        }

        public void EchoCancel(Span<byte> buffer)
        {
            ThrowIfDisposed();
            return;
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
                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NativeEchoCanceller));
        }
    }
}
