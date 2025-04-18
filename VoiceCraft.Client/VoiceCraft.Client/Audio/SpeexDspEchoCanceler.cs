using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio
{
    public class SpeexDspEchoCanceler : IEchoCanceler
    {
        public int FilterLengthMs { get; set; } = 100;

        public bool IsNative => false;
        private bool _disposed;
        private byte[]? _outputBuffer;
        private SpeexDSPEchoCanceler? _echoCanceler;
        
        ~SpeexDspEchoCanceler()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder, IAudioPlayer player)
        {
            ThrowIfDisposed();
            
            if(recorder.SampleRate != player.SampleRate)
                throw new ArgumentException("The specified audio recorder and audio player sample rate do not match!");
            
            CleanupEchoCanceler();

            _echoCanceler = new SpeexDSPEchoCanceler(
                recorder.BufferMilliseconds * recorder.SampleRate / 1000,
                FilterLengthMs * recorder.SampleRate / 1000,
                recorder.Channels,
                player.Channels);
            
            var sampleRate = recorder.SampleRate;
            _echoCanceler.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);
        }

        public void EchoCancel(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotIntialized();
            
            if(_outputBuffer == null || _outputBuffer.Length != buffer.Length)
                _outputBuffer = new byte[buffer.Length];
            else
                Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
            
            _echoCanceler?.EchoCapture(buffer, _outputBuffer);
            _outputBuffer.CopyTo(buffer);
        }

        public void EchoCancel(byte[] buffer) => EchoCancel(buffer.AsSpan());

        public void EchoPlayback(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotIntialized();

            _echoCanceler?.EchoPlayback(buffer);
        }

        public void EchoPlayback(byte[] buffer) => EchoPlayback(buffer.AsSpan());

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CleanupEchoCanceler()
        {
            if (_echoCanceler == null) return;
            _echoCanceler.Dispose();
            _echoCanceler = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(SpeexDspEchoCanceler));
        }

        private void ThrowIfNotIntialized()
        {
            if(_echoCanceler == null)
                throw new InvalidOperationException("Echo canceler is not initialized!");
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupEchoCanceler();
            _disposed = true;
        }
    }
}