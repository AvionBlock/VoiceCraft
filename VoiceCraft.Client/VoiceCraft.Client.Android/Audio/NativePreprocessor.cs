using Android.Media.Audiofx;
using System;
using VoiceCraft.Client.Audio.Interfaces;

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
                return _gainController?.Enabled ?? _gainControllerEnabled;
            }
            set
            {
                _gainController?.SetEnabled(value);
                _gainControllerEnabled = value;
            }
        }
        public bool NoiseSuppressorEnabled
        {
            get
            {
                if (!IsNoiseSuppressorAvailable) return false; //Will always be false regardless if not available.
                return _noiseSuppressor?.Enabled ?? _noiseSuppressorEnabled;
            }
            set
            {
                _noiseSuppressor?.SetEnabled(value);
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

            if (recorder is not AudioRecorder audioRecorder)
                throw new ArgumentException("Recorder must be an android native audio recorder!", nameof(recorder));
            CloseProcessors();
            _recorder = audioRecorder;
            _initialized = false;
            //Initialize variable is still false because we haven't created the processors, we create them when we start using the preprocessor.
        }

        public bool Process(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_initialized)
                return true; //Native android preprocessors don't allow us to see if voice is detected I don't think.
            if (_recorder?.SessionId == null) throw new InvalidOperationException("Native preprocessor must be intialized with a recorder!");
            OpenProcessors((int)_recorder.SessionId);
            _initialized = true;

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

            if (_noiseSuppressor == null) return;
            _noiseSuppressor.Release();
            _noiseSuppressor.Dispose();
            _noiseSuppressor = null;
        }

        private void OpenProcessors(int audioSession)
        {
            CloseProcessors();
            if (IsGainControllerAvailable)
            {
                _gainController = AutomaticGainControl.Create(audioSession);
                _gainController?.SetEnabled(_gainControllerEnabled);
            }

            if (!IsNoiseSuppressorAvailable) return;
            _noiseSuppressor = NoiseSuppressor.Create(audioSession);
            _noiseSuppressor?.SetEnabled(_noiseSuppressorEnabled);
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
                CloseProcessors();
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(NativeEchoCanceler));
        }
    }
}