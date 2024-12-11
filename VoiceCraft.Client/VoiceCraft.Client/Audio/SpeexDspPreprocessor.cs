using System;
 using NAudio.Wave;
 using SpeexDSPSharp.Core;
 using VoiceCraft.Client.Audio.Interfaces;

 namespace VoiceCraft.Client.Audio
{
    public class SpeexDspPreprocessor : IPreprocessor
    {
        private const int TargetGain = 15000;

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
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref state);
                }
            }
        }

        public bool NoiseSuppressorEnabled
        {
            get => _noiseSuppressorEnabled;
            set
            {
                _noiseSuppressorEnabled = value;
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref state);
                }
            }
        }

        public bool VoiceActivityDetectionEnabled
        {
            get => _voiceActivityDetectionEnabled;
            set
            {
                _voiceActivityDetectionEnabled = value;
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref state);
                }
            }
        }

        public bool Initialized => _preprocessors != null && _waveFormat != null;

        private bool _disposed;
        private bool _gainControllerEnabled;
        private bool _noiseSuppressorEnabled;
        private bool _voiceActivityDetectionEnabled;
        private WaveFormat? _waveFormat;
        private int _bytesPerFrame;
        private SpeexDSPPreprocessor[]? _preprocessors; //1 per channel apparently.
        private IAudioRecorder? _recorder;

        public void Init(IAudioRecorder recorder)
        {
            ThrowIfDisposed();

            if (_preprocessors != null)
            {
                var index = 0;
                for (; index < _preprocessors.Length; index++)
                {
                    var t = _preprocessors[index];
                    t.Dispose();
                }

                _preprocessors = null;
            }
            if (_recorder != null)
            {
                _recorder.DataAvailable -= OnDataAvailable;
            }
            
            _recorder = recorder;
            var preprocessors = new SpeexDSPPreprocessor[_recorder.WaveFormat.Channels];
            _waveFormat = _recorder.WaveFormat;
            _bytesPerFrame = _waveFormat.ConvertLatencyToByteSize(_recorder.BufferMilliseconds);
            try
            {
                for (var i = 0; i < preprocessors.Length; i++)
                {
                    preprocessors[i] = new SpeexDSPPreprocessor(_recorder.BufferMilliseconds * _waveFormat.SampleRate / 1000, _waveFormat.SampleRate); //1 per channel
                }

                foreach (var preprocessor in preprocessors)
                {
                    var gain = _gainControllerEnabled ? 1 : 0;
                    var noise = _noiseSuppressorEnabled ? 1 : 0;
                    var vad = _voiceActivityDetectionEnabled ? 1 : 0;
                    var targetGain = TargetGain;
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref gain);
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref noise);
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref vad);
                    preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref targetGain);
                }

                _preprocessors = preprocessors;
                _recorder.DataAvailable += OnDataAvailable;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to intialize {nameof(SpeexDSPPreprocessor)}.", ex);
            }
        }

        public bool Process(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_preprocessors == null || _waveFormat == null)
            {
                throw new InvalidOperationException("Speex preprocessor must be intialized with a recorder!");
            }
            if (buffer.Length < _bytesPerFrame)
            {
                throw new InvalidOperationException($"Input buffer must be {_bytesPerFrame} in length or higher!");
            }

            var channelCount = _waveFormat.Channels;
            var channelFrameSize = _bytesPerFrame / channelCount;

            // Allocate a single reusable buffer for channel frames
            Span<byte> channelFrames = new byte[channelFrameSize];

            var vad = false;
            for (var i = 0; i < channelCount; i++) // 1 preprocessor per channel
            {
                // Extract individual channel data from the interleaved buffer
                for (int j = i, frameIndex = 0; j < buffer.Length; j += channelCount, frameIndex++)
                {
                    channelFrames[frameIndex] = buffer[j];
                }

                // Run the associated preprocessor for the channel
                if (_preprocessors[i].Run(channelFrames) == 1)
                {
                    vad = true;
                }

                // Copy processed channel data back into the interleaved buffer
                for (int j = i, frameIndex = 0; j < buffer.Length; j += channelCount, frameIndex++)
                {
                    buffer[j] = channelFrames[frameIndex];
                }
                channelFrames.Clear();
            }

            return vad; //Will always be true according to speexdsp code if VAD is disabled.
        }

        public bool Process(byte[] buffer) => Process(buffer.AsSpan());
        
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_preprocessors == null || _waveFormat == null) return;
            Process(e.Buffer);
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
                if (_preprocessors != null)
                {
                    foreach (var t in _preprocessors)
                    {
                        t.Dispose();
                    }

                    _preprocessors = null;
                }
                if (_recorder != null)
                {
                    _recorder.DataAvailable -= OnDataAvailable;
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