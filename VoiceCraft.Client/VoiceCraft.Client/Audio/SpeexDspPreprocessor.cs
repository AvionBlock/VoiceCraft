using System;
 using NAudio.Wave;
 using SpeexDSPSharp.Core;
 using VoiceCraft.Client.Audio.Interfaces;

 namespace VoiceCraft.Client.Audio
{
    public class SpeexDspPreprocessor : IPreprocessor
    {
        private const int TargetGain = 24000;

        public bool IsNative => false;
        public bool IsGainControllerAvailable => true;
        public bool IsNoiseSuppressorAvailable => true;
        public bool IsVoiceActivityDetectionAvailable => true;

        public bool GainControllerEnabled
        {
            get => _gainControllerEnabled;
            set
            {
                _gainControllerEnabled = value;
                if (_preprocessor == null) return;
                var state = value ? 1 : 0;
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref state);
            }
        }

        public bool NoiseSuppressorEnabled
        {
            get => _noiseSuppressorEnabled;
            set
            {
                _noiseSuppressorEnabled = value;
                if (_preprocessor == null) return;
                var state = value ? 1 : 0;
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref state);
            }
        }

        public bool VoiceActivityDetectionEnabled
        {
            get => _voiceActivityDetectionEnabled;
            set
            {
                _voiceActivityDetectionEnabled = value;
                if (_preprocessor == null) return;
                var state = value ? 1 : 0;
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref state);
            }
        }

        private bool _disposed;
        private bool _gainControllerEnabled;
        private bool _noiseSuppressorEnabled;
        private bool _voiceActivityDetectionEnabled;
        private WaveFormat? _waveFormat;
        private int _bytesPerFrame;
        private SpeexDSPPreprocessor? _preprocessor;

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (_preprocessor != null)
            {
                _preprocessor.Dispose();
                _preprocessor = null;
            }

            if (recorder.WaveFormat.Channels != 1)
            {
                throw new NotSupportedException("Only a single channel is supported");
            }
            
            _waveFormat = recorder.WaveFormat;
            _bytesPerFrame = _waveFormat.ConvertLatencyToByteSize(recorder.BufferMilliseconds);
            try
            {
                _preprocessor = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * _waveFormat.SampleRate / 1000, _waveFormat.SampleRate);
                var gain = _gainControllerEnabled ? 1 : 0;
                var noise = _noiseSuppressorEnabled ? 1 : 0;
                var vad = _voiceActivityDetectionEnabled ? 1 : 0;
                var targetGain = TargetGain;
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref gain);
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref noise);
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref vad);
                _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref targetGain);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to intialize {nameof(SpeexDSPPreprocessor)}.", ex);
            }
        }

        public bool Process(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_preprocessor == null || _waveFormat == null)
            {
                throw new InvalidOperationException("Speex preprocessor must be intialized with a recorder!");
            }
            if (buffer.Length < _bytesPerFrame)
            {
                throw new InvalidOperationException($"Input buffer must be {_bytesPerFrame} in length or higher!");
            }

            var vad = _preprocessor.Run(buffer) == 1;

            return vad; //Will always be true according to speexdsp code if VAD is disabled.
        }

        public bool Process(byte[] buffer) => Process(buffer.AsSpan());

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
                if (_preprocessor != null)
                {
                    _preprocessor.Dispose();
                    _preprocessor = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(SpeexDSPPreprocessor));
        }
    }
}