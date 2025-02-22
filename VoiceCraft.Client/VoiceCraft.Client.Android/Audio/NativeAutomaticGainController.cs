using Android.Media.Audiofx;
using System;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAutomaticGainController : IAutomaticGainController
    {
        public bool IsNative => true;

        private AutomaticGainControl? _automaticGainControl;
        private AudioRecorder? _recorder;
        private bool _initialized;
        private bool _disposed;

        ~NativeAutomaticGainController()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (recorder is not AudioRecorder audioRecorder)
                throw new ArgumentException(Locales.Locales.Android_NativeAGC_Exception_AndroidRecorder, nameof(recorder));
            if (_automaticGainControl != null)
            {
                _automaticGainControl.Release();
                _automaticGainControl.Dispose();
                _automaticGainControl = null;
            }

            _recorder = audioRecorder;
            _initialized = false;
        }

        public void Process(byte[] buffer) => Process(buffer.AsSpan());

        public void Process(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_initialized) return;
            if (_recorder?.SessionId == null)
                throw new InvalidOperationException(Locales.Locales.Android_NativeAGC_Exception_Init);
            if (_automaticGainControl != null)
            {
                _automaticGainControl.Release();
                _automaticGainControl.Dispose();
                _automaticGainControl = null;
            }
            
            _automaticGainControl = AutomaticGainControl.Create((int)_recorder.SessionId);
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
                if (_automaticGainControl != null)
                {
                    _automaticGainControl.Release();
                    _automaticGainControl.Dispose();
                    _automaticGainControl = null;
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