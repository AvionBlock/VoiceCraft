using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio
{
    public class SpeexDspDenoiser : IDenoiser
    {
        public bool IsNative => false;
        private SpeexDSPPreprocessor? _denoiser;
        private bool _disposed;

        ~SpeexDspDenoiser()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            ThrowIfDisposed();
            
            if(recorder.Channels != 1)
                throw new InvalidOperationException("Speex denoiser can only support mono audio channels!");

            CleanupDenoiser();
            
            _denoiser = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.SampleRate / 1000, recorder.SampleRate);
            
            var @false = 0;
            var @true = 1;
            _denoiser.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref @false);
            _denoiser.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref @false);
            _denoiser.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref @false);
            _denoiser.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref @true);
        }

        public void Denoise(byte[] buffer) => Denoise(buffer.AsSpan());
        
        public void Denoise(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotIntialized();
            _denoiser?.Run(buffer);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupDenoiser()
        {
            if (_denoiser == null) return;
            _denoiser.Dispose();
            _denoiser = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(SpeexDspDenoiser).ToString());
        }

        private void ThrowIfNotIntialized()
        {
            if(_denoiser == null)
                throw new InvalidOperationException("Denoiser is not initialized!");
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupDenoiser();
            _disposed = true;
        }
    }
}