using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio
{
    public class SpeexDspAutomaticGainController : IAutomaticGainController
    {
        public bool IsNative => false;
        private SpeexDSPPreprocessor? _automaticGainControllerPreprocessor;
        private bool _disposed;
        
        ~SpeexDspAutomaticGainController()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            //Disposed? DIE!!
            ThrowIfDisposed();

            //Close previous preprocessor
            if (_automaticGainControllerPreprocessor != null)
            {
                _automaticGainControllerPreprocessor.Dispose();
                _automaticGainControllerPreprocessor = null;
            }
            
            //Check if recorder is mono channel.
            if(recorder.Channels != 1)
                throw new InvalidOperationException("Speex denoiser can only support mono audio channels!");
            
            //Create preprocessor
            _automaticGainControllerPreprocessor = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.SampleRate / 1000, recorder.SampleRate);

            //Setup preprocessor to only work with the denoiser.
            var @false = 0;
            var @true = 1;
            var targetGain = 18000;
            _automaticGainControllerPreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref @true);
            _automaticGainControllerPreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref @false);
            _automaticGainControllerPreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref @false);
            _automaticGainControllerPreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref @false);
            _automaticGainControllerPreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref targetGain);
        }

        public void Process(byte[] buffer) => Process(buffer.AsSpan());
        
        public void Process(Span<byte> buffer)
        {
            //Disposed? DIE!!
            ThrowIfDisposed();
            
            //Check if the preprocessor has been initialized.
            if (_automaticGainControllerPreprocessor == null)
                throw new InvalidOperationException("Speex automatic gain controller must be intialized with a recorder!");
            
            _automaticGainControllerPreprocessor.Run(buffer);
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
                if (_automaticGainControllerPreprocessor != null)
                {
                    _automaticGainControllerPreprocessor.Dispose();
                    _automaticGainControllerPreprocessor = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(SpeexDspDenoiser));
        }
    }
}