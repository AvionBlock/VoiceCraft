using System;
using System.Threading;
using System.Threading.Tasks;
using Ownaudio.Core;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Audio;

public class AudioPlayer
{
    private readonly IAudioEngine _audioEngine;
    private readonly Lock _lock = new();
    private readonly float[] _buffer;
    
    private Func<Span<float>, int>? _sourceCallback;
    private bool _isPlaying;
    
    public AudioPlayer()
    {
        var config = new AudioConfig
        {
            SampleRate = Constants.SampleRate,
            BufferSize = Constants.FrameSize,
            Channels = Constants.PlaybackChannels,
            EnableInput = false,
            EnableOutput = true
        };

        _buffer = new float[config.BufferSize * config.Channels];

        _audioEngine = AudioEngineFactory.Create(config);
    }

    public void StartPlaying(Func<Span<float>, int> sourceCallback)
    {
        if (_isPlaying) return;
        lock (_lock)
        {
            _audioEngine.Start();
            _sourceCallback = sourceCallback;
            _isPlaying = true;
            Task.Run(RecordingLogic);
        }
    }

    public void StopPlaying()
    {
        if(!_isPlaying) return;
        lock (_lock)
        {
            _audioEngine.Stop();
            _isPlaying = false;
        }
    }

    private void RecordingLogic()
    {
        var sw = new SpinWait();
        while (_isPlaying && _sourceCallback != null)
        {
            var samplesReceived = _sourceCallback(_buffer.AsSpan());
            if (samplesReceived > 0)
            {
                _audioEngine.Send(_buffer.AsSpan(0, samplesReceived));
            }
            sw.SpinOnce();
        }
    }
}