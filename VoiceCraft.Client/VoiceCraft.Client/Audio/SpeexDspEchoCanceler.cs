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
        private byte[] _outputBuffer = [];
        private byte[] _captureBuffer = [];
        private int _captureBufferIndex;
        private SpeexDSPEchoCanceler? _echoCanceler;
        
        ~SpeexDspEchoCanceler()
        {
            Dispose(false);
        }

        public void Initialize(IAudioRecorder recorder, IAudioPlayer player)
        {
            ThrowIfDisposed();
            
            if(recorder.SampleRate != player.SampleRate)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_InitFailed);
            
            CleanupEchoCanceler();

            var bufferSamples = recorder.BufferMilliseconds * recorder.SampleRate / 1000; //Calculate buffer size IN SAMPLES!
            var bufferBytes = recorder.BitDepth / 8 * recorder.Channels * bufferSamples;
            _echoCanceler = new SpeexDSPEchoCanceler(
                bufferSamples,
                FilterLengthMs * recorder.SampleRate / 1000,
                recorder.Channels,
                player.Channels);
            _captureBuffer = new byte[bufferBytes];
            _outputBuffer = new byte[bufferBytes];
            _captureBufferIndex = 0;
            
            var sampleRate = recorder.SampleRate;
            _echoCanceler.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);
        }

        public void EchoCancel(Span<byte> buffer, int count)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ArgumentOutOfRangeException.ThrowIfLessThan(count, _outputBuffer.Length);
            Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
            
            _echoCanceler?.EchoCapture(buffer, _outputBuffer);
            _outputBuffer.CopyTo(buffer);
        }

        public void EchoCancel(byte[] buffer, int count) => EchoCancel(buffer.AsSpan(), count);

        public void EchoPlayback(Span<byte> buffer, int count)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            var offset = 0;

            while (offset != count)
            {
                var bytesAdded = AddBytes(buffer, offset, count);
                if (_captureBufferIndex >= _captureBuffer.Length)
                {
                    _echoCanceler?.EchoPlayback(_captureBuffer); //Add the full audio frame.
                    Array.Clear(_captureBuffer, 0, _captureBuffer.Length);
                    _captureBufferIndex = 0;
                }
                offset += bytesAdded;
            }
        }

        public void EchoPlayback(byte[] buffer, int count) => EchoPlayback(buffer.AsSpan(), count);

        private int AddBytes(Span<byte> buffer, int offset, int count)
        {
            if(offset == count) return 0;
            var amountToCopy = Math.Min(_captureBuffer.Length - _captureBufferIndex, count - offset);
            buffer.Slice(offset, amountToCopy).CopyTo(_captureBuffer.AsSpan()[_captureBufferIndex..]);
            _captureBufferIndex += amountToCopy;
            return amountToCopy;
        }

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
            throw new ObjectDisposedException(typeof(SpeexDspEchoCanceler).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_echoCanceler == null)
                throw new InvalidOperationException(Locales.Locales.Audio_AEC_Init);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            CleanupEchoCanceler();
            _disposed = true;
        }
    }
}