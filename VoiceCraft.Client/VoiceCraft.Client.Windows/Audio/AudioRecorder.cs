using NAudio.Wave;
using System;
using NAudio.CoreAudioApi;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _disposed;
        private readonly WaveInEvent _nativeRecorder = new();
        
        public WaveFormat WaveFormat { get => _nativeRecorder.WaveFormat; set => _nativeRecorder.WaveFormat = value; }
        public int NumberOfBuffers { get => _nativeRecorder.NumberOfBuffers; set => _nativeRecorder.NumberOfBuffers = value; }
        public int BufferMilliseconds { get => _nativeRecorder.BufferMilliseconds; set => _nativeRecorder.BufferMilliseconds = value; }
        public CaptureState CaptureState { get; private set; }
        public string? SelectedDevice { get; set; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder()
        {
            _nativeRecorder.DataAvailable += InvokeDataAvailable;
            _nativeRecorder.RecordingStopped += InvokeRecordingStopped;
        }

        public void StartRecording()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            var selectedDevice = -1;
            for (var n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                if (caps.ProductName != SelectedDevice) continue;
                selectedDevice = n;
                break;
            }

            CaptureState = CaptureState.Starting;
            _nativeRecorder.DeviceNumber = selectedDevice;
            _nativeRecorder.StartRecording();
        }

        public void StopRecording()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            CaptureState = CaptureState.Stopping;
            _nativeRecorder.StopRecording();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void InvokeDataAvailable(object? sender, WaveInEventArgs e)
        {
            if(CaptureState != CaptureState.Capturing)
                CaptureState = CaptureState.Capturing;
            DataAvailable?.Invoke(sender, e);
        }

        private void InvokeRecordingStopped(object? sender, StoppedEventArgs e)
        {
            CaptureState = CaptureState.Stopped;
            RecordingStopped?.Invoke(sender, e);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;
            
            _nativeRecorder.RecordingStopped -= InvokeRecordingStopped;
            _nativeRecorder.DataAvailable -= InvokeDataAvailable;
            _nativeRecorder.Dispose();
            _disposed = true;
        }
    }
}