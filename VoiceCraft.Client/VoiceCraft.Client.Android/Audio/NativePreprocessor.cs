using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativePreprocessor : IPreprocessor
    {
        public bool IsNative => true;
        public bool IsGainControllerAvailable => AutomaticGainControl.IsAvailable;
        public bool IsNoiseSuppressorAvailable => NoiseSuppressor.IsAvailable;
        public bool IsVoiceActivityDetectionAvailable => false; //Nope

        public bool GainControllerEnabled
        {
            get
            {
                if (!IsGainControllerAvailable) return false; //Will always be false regardless if not available.
                if (_gainController != null) return _gainController.Enabled;
                return _gainControllerEnabled;
            }
            set
            {
                if (_gainController != null) _gainController.SetEnabled(value);
                _gainControllerEnabled = value;
            }
        }
        public bool NoiseSuppressorEnabled
        {
            get
            {
                if (!IsNoiseSuppressorAvailable) return false; //Will always be false regardless if not available.
                if (_noiseSuppressor != null) return _noiseSuppressor.Enabled;
                return _noiseSuppressorEnabled;
            }
            set
            {
                if (_noiseSuppressor != null) _noiseSuppressor.SetEnabled(value);
                _noiseSuppressorEnabled = value;
            }
        }

        public bool VoiceActivityDetectionEnabled { get => false; set { } } //Set does absolutely nothing.

        public bool Initialized => _recorder != null;

        private bool _disposed;
        private bool _initialized;
        private bool _gainControllerEnabled;
        private bool _noiseSuppressorEnabled;
        private AutomaticGainControl? _gainController;
        private NoiseSuppressor? _noiseSuppressor;
        private AudioRecorder? _recorder;

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (recorder is AudioRecorder audioRecorder)
            {
                CloseProcessors();
                _recorder = audioRecorder;
                _initialized = false;
                return;
            }

            throw new ArgumentException("Recorder must be an android native audio recorder!", nameof(recorder));
            //Initialize variable is still false because we haven't created the processors, we create them when we start using the preprocessor.
        }

        public bool Process(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (!_initialized)
            {
                if (_recorder?.SessionId == null) throw new InvalidOperationException("Native preprocessor must be intialized with a recorder!");
                OpenProcessors((int)_recorder.SessionId);
                _initialized = true;
            }

            return true; //Native android preprocessors don't allow us to see if voice is detected I don't think.
            //Usually we don't actually need to use this multiple times since the native android preprocessors attached to the recorder will automatically do it for us.
        }

        public bool Process(byte[] buffer) => Process(buffer.AsSpan());

        private void CloseProcessors()
        {
            if (_gainController != null)
            {
                _gainController.Release();
                _gainController.Dispose();
                _gainController = null;
            }

            if(_noiseSuppressor != null)
            {
                _noiseSuppressor.Release();
                _noiseSuppressor.Dispose();
                _noiseSuppressor = null;
            }
        }

        private void OpenProcessors(int audioSession)
        {
            CloseProcessors();
            if (IsGainControllerAvailable)
            {
                _gainController = AutomaticGainControl.Create(audioSession);
                _gainController?.SetEnabled(_gainControllerEnabled);
            }

            if (IsNoiseSuppressorAvailable)
            {
                _noiseSuppressor = NoiseSuppressor.Create(audioSession);
                _noiseSuppressor?.SetEnabled(_noiseSuppressorEnabled);
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
                CloseProcessors();
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