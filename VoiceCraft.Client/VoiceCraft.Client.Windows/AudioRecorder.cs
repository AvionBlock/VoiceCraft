using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _isRecording;
        private WaveInEvent _nativeRecorder = new WaveInEvent();

        public bool IsRecording => _isRecording;
        public WaveFormat WaveFormat { get => _nativeRecorder.WaveFormat; set => _nativeRecorder.WaveFormat = value; }
        public int BufferMilliseconds { get => _nativeRecorder.BufferMilliseconds; set => _nativeRecorder.BufferMilliseconds = value; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder()
        {
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
            _isRecording = false;
            RecordingStopped?.Invoke(sender, e);
        }
    }
}
