using NAudio.Wave;
using System;
using System.Collections.Generic;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        private bool _isRecording;
        private string? _selectedDevice;
        private readonly WaveInEvent _nativeRecorder = new WaveInEvent();

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

        public void StartRecording()
        {
            var selectedDevice = -1;
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                if (caps.ProductName == _selectedDevice)
                {
                    selectedDevice = n;
                    break;
                }
            }

            _nativeRecorder.DeviceNumber = selectedDevice;
            _nativeRecorder.StartRecording();
            _isRecording = true;
        }

        public void StopRecording()
        {
            _nativeRecorder.StopRecording();
            _isRecording = false;
        }

        public void SetDevice(string device)
        {
            _selectedDevice = device;
        }

        public string GetDefaultDevice()
        {
            return "Default";
        }

        public List<string> GetDevices()
        {
            var devices = new List<string>() { GetDefaultDevice() };
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                if(!devices.Contains(caps.ProductName))
                    devices.Add(caps.ProductName);
            }

            return devices;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nativeRecorder.Dispose();
            }
        }
    }
}
