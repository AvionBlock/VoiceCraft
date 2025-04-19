using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio
{
    public class SpeexDspAutomaticGainController : IAutomaticGainController
    {
        public bool IsNative => false;
        private SpeexDSPPreprocessor? _gainController;
        private bool _disposed;
        
        ~SpeexDspAutomaticGainController()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder)
        {
            ThrowIfDisposed();
            
            if(recorder.Channels != 1)
                throw new InvalidOperationException("Speex denoiser can only support mono audio channels!");
            
            CleanupGainController();
            
            _gainController = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.SampleRate / 1000, recorder.SampleRate);
            
            var @false = 0;
            var @true = 1;
            var targetGain = 21000;
            _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref @true);
            _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref @false);
            _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref @false);
            _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref @false);
            _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref targetGain);
        }

        public void Process(byte[] buffer) => Process(buffer.AsSpan());
        
        public void Process(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotIntialized();
            _gainController?.Run(buffer);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupGainController()
        {
            if (_gainController == null) return;
            _gainController.Dispose();
            _gainController = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(SpeexDspAutomaticGainController).ToString());
        }
        
        private void ThrowIfNotIntialized()
        {
            if(_gainController == null)
                throw new InvalidOperationException("Automatic gain controller is not initialized!");
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupGainController();
            _disposed = true;
        }
    }
}