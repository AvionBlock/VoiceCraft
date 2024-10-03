using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _isRecording;
        private IWaveIn _nativeRecorder;

        public IWaveIn NativeRecorder => _nativeRecorder;
        public bool IsRecording => _isRecording;
        public WaveFormat WaveFormat { get => NativeRecorder.WaveFormat; set => NativeRecorder.WaveFormat = value; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder()
        {
            _nativeRecorder = new WaveInEvent();
            _nativeRecorder.DataAvailable += (s, e) => DataAvailable?.Invoke(s, e);
            _nativeRecorder.RecordingStopped += (s, e) => RecordingStopped?.Invoke(s, e);
        }

        public void Dispose()
        {
            _nativeRecorder.Dispose();
        }

        public void SetDevice(string device)
        {
            throw new NotImplementedException();
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
    }
}
