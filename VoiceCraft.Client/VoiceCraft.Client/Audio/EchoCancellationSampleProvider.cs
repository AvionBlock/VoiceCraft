using System;
using NAudio.Wave;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Audio
{
    public class EchoCancellationSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat => _source.WaveFormat;
        private readonly IEchoCanceler _canceller;
        private readonly ISampleProvider _source;
        private readonly byte[] _buffer;
        
        public EchoCancellationSampleProvider(int bufferMilliseconds, ISampleProvider source, IEchoCanceler echoCanceler)
        {
            _canceller = echoCanceler;
            _source = source;
            var bytesPerFrame = WaveFormat.ConvertLatencyToByteSize(bufferMilliseconds);
            _buffer = new byte[bytesPerFrame];
        }

        public void Init(IAudioRecorder audioRecorder, IAudioPlayer audioPlayer)
        {
            _canceller.Init(audioRecorder, audioPlayer);
        }
        
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            Buffer.BlockCopy(buffer, 0, _buffer, 0, count);
            _canceller.EchoPlayback(_buffer);
            return read;
        }

        public void Cancel(byte[] buffer)
        {
            _canceller.EchoCancel(buffer);
        }
    }
}