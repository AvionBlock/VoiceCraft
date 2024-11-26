using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public class SpeexDSPPreprocessor : IPreprocessor
    {
        public bool IsNative => false;
        public bool IsGainControllerAvailable => true;
        public bool IsNoiseSuppressorAvailable => true;
        public bool IsVoiceActivityDetectionAvailable => true;

        public bool GainControllerEnabled {
            get
            {
                return _gainControllerEnabled;
            }
            set 
            {
                _gainControllerEnabled = value;
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(SpeexDSPSharp.Core.PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref state);
                }
            }
        }
        public bool NoiseSuppressorEnabled
        {
            get
            {
                return _noiseSuppressorEnabled;
            }
            set
            {
                _noiseSuppressorEnabled = value;
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(SpeexDSPSharp.Core.PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref state);
                }
            }
        }
        public bool VoiceActivityDetectionEnabled
        {
            get
            {
                return _voiceActivityDetectionEnabled;
            }
            set
            {
                _voiceActivityDetectionEnabled = value;
                if (_preprocessors == null) return;
                foreach (var preprocessor in _preprocessors)
                {
                    var state = value ? 1 : 0;
                    preprocessor.Ctl(SpeexDSPSharp.Core.PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref state);
                }
            }
        }

        private bool _disposed;
        private bool _gainControllerEnabled;
        private bool _noiseSuppressorEnabled;
        private bool _voiceActivityDetectionEnabled;
        private WaveFormat? _waveFormat;
        private int _bytesPerFrame;
        private SpeexDSPSharp.Core.SpeexDSPPreprocessor[]? _preprocessors; //1 per channel apparently.

        public void Init(IAudioRecorder recorder)
        {
            if(_preprocessors != null)
            {
                for (int i = 0; i < _preprocessors.Length; i++)
                {
                    _preprocessors[i].Dispose();
                }
                _preprocessors = null;
            }

            _preprocessors = new SpeexDSPSharp.Core.SpeexDSPPreprocessor[recorder.WaveFormat.Channels];
            _waveFormat = recorder.WaveFormat;
            _bytesPerFrame = _waveFormat.ConvertLatencyToByteSize(recorder.BufferMilliseconds);
            for(int i = 0; i < _preprocessors.Length; i++)
            {
                _preprocessors[i] = new SpeexDSPSharp.Core.SpeexDSPPreprocessor(recorder.BufferMilliseconds * _waveFormat.SampleRate / 1000, _waveFormat.SampleRate); //1 per channel
            }

            //I don't give a fuck, this is easier than having to add a whole bunch of new code.
            GainControllerEnabled = _gainControllerEnabled;
            NoiseSuppressorEnabled = _noiseSuppressorEnabled;
            VoiceActivityDetectionEnabled = _voiceActivityDetectionEnabled;
        }

        public bool Process(Span<byte> buffer)
        {
            if(_preprocessors == null || _waveFormat == null)
            {
                throw new InvalidOperationException("Speex preprocessor must be intialized with a recorder!");
            }
            if(buffer.Length < _bytesPerFrame)
            {
                throw new InvalidOperationException($"Input buffer must be {_bytesPerFrame} in length or higher!");
            }

            var channelCount = _waveFormat.Channels;
            var channelFrameSize = _bytesPerFrame / channelCount;

            // Allocate a single reusable buffer for channel frames
            Span<byte> channelFrames = new byte[channelFrameSize];

            var vad = false;
            for (int i = 0; i < channelCount; i++) // 1 preprocessor per channel
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
                if (_preprocessors != null)
                {
                    for (int i = 0; i < _preprocessors.Length; i++)
                    {
                        _preprocessors[i].Dispose();
                    }
                    _preprocessors = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SpeexDSPPreprocessor));
        }
    }
}
