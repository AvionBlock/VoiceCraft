using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio
{
    public class SpeexDspDenoiser : IDenoiser
    {
        public bool IsNative => false;
        private SpeexDSPPreprocessor? _denoisePreprocessor;
        private bool _disposed;

        ~SpeexDspDenoiser()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            //Disposed? DIE!!
            ThrowIfDisposed();

            //Close previous preprocessor
            if (_denoisePreprocessor != null)
            {
                _denoisePreprocessor.Dispose();
                _denoisePreprocessor = null;
            }
            
            //Check if recorder is mono channel.
            if(recorder.Channels != 1)
                throw new InvalidOperationException("Speex denoiser can only support mono audio channels!");
            
            //Create preprocessor
            _denoisePreprocessor = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.SampleRate / 1000, recorder.SampleRate);

            //Setup preprocessor to only work with the denoiser.
            var @false = 0;
            var @true = 1;
            _denoisePreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref @false);
            _denoisePreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref @false);
            _denoisePreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref @false);
            _denoisePreprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref @true);
        }

        public void Denoise(byte[] buffer) => Denoise(buffer.AsSpan());
        public void Denoise(Span<byte> buffer)
        {
            //Disposed? DIE!!
            ThrowIfDisposed();
            
            //Check if the preprocessor has been initialized.
            if (_denoisePreprocessor == null)
                throw new InvalidOperationException("Speex denoiser must be intialized with a recorder!");
            
            _denoisePreprocessor.Run(buffer);
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
                if (_denoisePreprocessor != null)
                {
                    _denoisePreprocessor.Dispose();
                    _denoisePreprocessor = null;
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