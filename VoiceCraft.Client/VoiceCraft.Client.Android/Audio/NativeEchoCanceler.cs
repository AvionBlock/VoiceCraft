using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeEchoCanceler : IEchoCanceler
    {
        public bool IsNative => true;

        public bool IsAvailable => AcousticEchoCanceler.IsAvailable;

        public bool Enabled {
            get
            {
                if (!IsAvailable) return false; //Will always be false regardless if not available.
                if (_echoCanceler != null) return _echoCanceler.Enabled;
                return _enabled;
            }
            set
            {
                if (_echoCanceler != null) _echoCanceler.SetEnabled(value);
                _enabled = value;
            }
        }

        public bool Initialized => _recorder != null;

        private AcousticEchoCanceler? _echoCanceler;
        private AudioRecorder? _recorder;
        private bool _enabled = true;
        private bool _initialized;
        private bool _disposed;

        ~NativeEchoCanceler()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder, IAudioPlayer player) //We don't need have the audio player but it's there for other compatibility reasons.
        {
            ThrowIfDisposed();

            if (recorder is AudioRecorder audioRecorder)
            {
                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }

                _recorder = audioRecorder;
                _initialized = false;
                return;
            }

            throw new ArgumentException("Recorder must be an android native audio recorder!", nameof(recorder));
        }

        public void EchoPlayback(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (!_initialized)
            {
                if (_recorder?.SessionId == null) throw new InvalidOperationException("Native preprocessor must be intialized with a recorder!");

                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }

                if (IsAvailable)
                    _echoCanceler = AcousticEchoCanceler.Create((int)_recorder.SessionId);

                _initialized = true;
            }
        }

        public void EchoPlayback(byte[] buffer) => EchoPlayback(buffer.AsSpan());

        public void EchoCancel(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (!_initialized)
            {
                if (_recorder?.SessionId == null) throw new InvalidOperationException("Native preprocessor must be intialized with a recorder!");
                if (_echoCanceler != null)
                {
                    _echoCanceler.Release();
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }

                if (IsAvailable)
                    _echoCanceler = AcousticEchoCanceler.Create((int)_recorder.SessionId);

                _initialized = true;
            }
        }

        public void EchoCancel(byte[] buffer) => EchoCancel(buffer.AsSpan());

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
                throw new ObjectDisposedException(nameof(NativeEchoCanceler));
        }
    }
}
