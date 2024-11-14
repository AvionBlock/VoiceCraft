using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAGC
    {
        public bool Enabled
        {
            get => _automaticGainController?.Enabled ?? _enabled; set
            {
                if (_automaticGainController != null)
                    _automaticGainController.SetEnabled(value);
                else
                    _enabled = value;
            }
        }
        private AutomaticGainControl? _automaticGainController;
        private bool _enabled = true;
        private bool _disposed;

        ~NativeAGC()
        {
            Dispose(false);
        }

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();
            if (recorder is AudioRecorder audioRecorder && audioRecorder.SessionId != null)
            {
                if (_automaticGainController != null)
                {
                    _automaticGainController.Release();
                    _automaticGainController.Dispose();
                    _automaticGainController = null;
                }

                _automaticGainController = AutomaticGainControl.Create((int)audioRecorder.SessionId);
                _automaticGainController?.SetEnabled(_enabled); //Force setting of AGC.
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
                if (_automaticGainController != null)
                {
                    _automaticGainController.Release();
                    _automaticGainController.Dispose();
                    _automaticGainController = null;
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
