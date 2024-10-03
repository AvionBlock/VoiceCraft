using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _isRecording;
        private WaveInEvent _nativeRecorder;

        public IWaveIn NativeRecorder => _nativeRecorder;
        public bool IsRecording => _isRecording;
        public WaveFormat WaveFormat { get => NativeRecorder.WaveFormat; set => NativeRecorder.WaveFormat = value; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder()
        {
            _nativeRecorder = new WaveInEvent()
            {
                BufferMilliseconds = IAudioRecorder.BufferMilliseconds,
                WaveFormat = IAudioRecorder.RecordFormat
            };
            _nativeRecorder.DataAvailable += InvokeDataAvailable;
            _nativeRecorder.RecordingStopped += InvokeRecordingStopped;
        }

        public void Dispose()
        {
            _nativeRecorder.Dispose();
        }

        public void SetDevice(string device)
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                if(caps.ProductName == device)
                {
                    _nativeRecorder.DeviceNumber = n;
                    return;
                }
            }

            _nativeRecorder.DeviceNumber = -1;
        }

        public void StartRecording()
        {
            _nativeRecorder.StartRecording();
            _isRecording = true;
        }

        public void StopRecording()
        {
            _nativeRecorder.StopRecording();
            _isRecording = false;
        }

        private void InvokeDataAvailable(object? sender, WaveInEventArgs e)
        {
            DataAvailable?.Invoke(sender, e);
        }

        private void InvokeRecordingStopped(object? sender, StoppedEventArgs e)
        {
            RecordingStopped?.Invoke(sender, e);
        }
    }
}
