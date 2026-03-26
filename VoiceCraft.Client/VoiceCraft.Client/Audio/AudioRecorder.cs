using System;
using System.Threading;
using System.Threading.Tasks;
using Ownaudio.Core;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Audio;

public class AudioRecorder
{
    private readonly IAudioEngine _audioEngine;
    private readonly Lock _lock = new();
    
    private Action<Span<float>>? _recordCallback;
    private bool _isRecording;

    public AudioRecorder()
    {
        var config = new AudioConfig
        {
            SampleRate = Constants.SampleRate,
            BufferSize = Constants.FrameSize,
            Channels = Constants.RecordingChannels,
            EnableInput = true,
            EnableOutput = false
        };
        
        _audioEngine = AudioEngineFactory.Create(config);
    }

    public void StartRecording(Action<Span<float>> recordCallback)
    {
        if (_isRecording) return;
        lock (_lock)
        {
            _audioEngine.Start();
            _recordCallback = recordCallback;
            _isRecording = true;
            Task.Run(RecordingLogic);
        }
    }

    public void StopRecording()
    {
        if(!_isRecording) return;
        lock (_lock)
        {
            _audioEngine.Stop();
            _isRecording = false;
        }
    }

    private void RecordingLogic()
    {
        var sw = new SpinWait();
        while (_isRecording)
        {
            var samplesReceived = _audioEngine.Receives(out var samples);
            switch (samplesReceived)
            {
                case > 0:
                    _recordCallback?.Invoke(samples.AsSpan(0, samplesReceived));
                    break;
                case -1:
                    StopRecording();
                    break;
            }
            sw.SpinOnce();
        }
    }
}