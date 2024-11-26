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
        private SpeexDSPSharp.Core.SpeexDSPPreprocessor[]? _preprocessors; //1 per channel apparently.

        public void Init(IAudioRecorder recorder) //We don't actually need to do this.
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
            for(int i = 0; i < _preprocessors.Length; i++)
            {
                _preprocessors[i] = new SpeexDSPSharp.Core.SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.WaveFormat.SampleRate / 1000, recorder.WaveFormat.SampleRate); //1 per channel
            }

            //I don't give a fuck, this is easier than having to add a whole bunch of new code.
            GainControllerEnabled = _gainControllerEnabled;
            NoiseSuppressorEnabled = _noiseSuppressorEnabled;
            VoiceActivityDetectionEnabled = _voiceActivityDetectionEnabled;
        }

        public bool Process(Span<byte> buffer)
        {
            if(_preprocessors == null)
            {
                throw new InvalidOperationException("Speex preprocessor must be intialized with a recorder!");
            }

            var vad = false;
            for (int i = 0; i < _preprocessors.Length; i++) //1 per channel
            {
                var frames = new byte[buffer.Length / _preprocessors.Length]; //Individual Channel
                var frameIndex = 0;
                //Take out the individual channel from the interleaved buffer.
                for (int j = i; j < buffer.Length; j += _preprocessors.Length)
                {
                    frames[frameIndex] = buffer[j];
                    frameIndex++;
                }

                //Run the associated preprocessor to the channel.
                var preprocessor = _preprocessors[i];
                vad = preprocessor.Run(frames) == 1;
            }

            return vad; //Will always be true according to speexdsp code.
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
