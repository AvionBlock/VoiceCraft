using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio
{
    public class SpeexDspEchoCanceler : IEchoCanceler
    {
        private const int FilterLengthMs = 100;

        public bool IsNative => false;
        private bool _disposed;
        private int _bytesPerFrame;
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
            
            if (_echoCanceler != null)
            {
                _echoCanceler.Dispose();
                _echoCanceler = null;
            }
            
            _bytesPerFrame = recorder.BufferMilliseconds;

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

            if (_echoCanceler == null)
                throw new InvalidOperationException("Speex echo canceller must be intialized with a recorder!");
            if (buffer.Length < _bytesPerFrame)
                throw new InvalidOperationException($"Input buffer must be {_bytesPerFrame} in length or higher!");
            
            if(_outputBuffer == null || _outputBuffer.Length != buffer.Length)
                _outputBuffer = new byte[buffer.Length];
            else
                Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
            
            _echoCanceler.EchoCapture(buffer, _outputBuffer);
            _outputBuffer.CopyTo(buffer);
        }

        public void EchoCancel(byte[] buffer) => EchoCancel(buffer.AsSpan());

        public void EchoPlayback(Span<byte> buffer)
        {
            ThrowIfDisposed();

            if (_echoCanceler == null)
                throw new InvalidOperationException("Speex echo canceller must be intialized with a recorder!");

            _echoCanceler.EchoPlayback(buffer);
        }

        public void EchoPlayback(byte[] buffer) => EchoPlayback(buffer.AsSpan());

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
                if (_echoCanceler != null)
                {
                    _echoCanceler.Dispose();
                    _echoCanceler = null;
                }
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(SpeexDspEchoCanceler));
        }
    }
}