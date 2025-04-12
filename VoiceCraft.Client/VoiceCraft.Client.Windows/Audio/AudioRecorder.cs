using NAudio.Wave;
using System;
using System.Threading;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        //Public Properties
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if(CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException("Cannot set sample rate when recording!");
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Sample rate must be greater than or equal to zero!");

                _sampleRate = value;
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                if(CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException("Cannot set channels when recording!");
                if(value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }

        public int BitDepth
        {
            get
            {
                return (Format) switch
                {
                    AudioFormat.Pcm8 => 8,
                    AudioFormat.Pcm16 => 16,
                    AudioFormat.PcmFloat => 32,
                    _ => throw new ArgumentOutOfRangeException(nameof(Format))
                };
            }
        }

        public AudioFormat Format
        {
            get => _format;
            set
            {
                if(CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException("Cannot set audio format when recording!");
                
                _format = value;
            }
        }

        public int BufferMilliseconds
        {
            get => _bufferMilliseconds;
            set
            {
                if(CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException("Cannot set buffer milliseconds when recording!");
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if(CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException("Cannot set selected device when recording!");
                
                _selectedDevice = value;
            }
        }

        public CaptureState CaptureState { get; private set; }

        public event Action<byte[], int>? OnDataAvailable;
        public event Action<Exception?>? OnRecordingStopped;
        
        //Privates
        private WaveInEvent? _nativeRecorder;
        private int _sampleRate;
        private int _channels;
        private AudioFormat _format;
        private int _bufferMilliseconds;
        private string? _selectedDevice;
        private bool _disposed;

        public AudioRecorder(int sampleRate, int channels, AudioFormat format)
        {
            SampleRate = sampleRate;
            Channels = channels;
            Format = format;
        }
        
        ~AudioRecorder()
        {
            //Dispose of this object.
            Dispose(false);
        }

        public void Initialize()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            if(CaptureState != CaptureState.Stopped)
                throw new InvalidOperationException("Cannot initialize when recording!");
            
            //Cleanup previous recorder.
            CleanupRecorder();

            try
            {
                //Select Device.
                var selectedDevice = -1;
                for (var n = 0; n < WaveIn.DeviceCount; n++)
                {
                    var caps = WaveIn.GetCapabilities(n);
                    if (caps.ProductName != SelectedDevice) continue;
                    selectedDevice = n;
                    break;
                }

                //Setup WaveFormat
                var waveFormat = Format switch
                {
                    AudioFormat.Pcm8 => new WaveFormat(SampleRate, 8, Channels),
                    AudioFormat.Pcm16 => new WaveFormat(SampleRate, 16, Channels),
                    AudioFormat.PcmFloat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels),
                    _ => throw new NotSupportedException("Input format is not supported!")
                };

                //Setup Recorder.
                _nativeRecorder = new WaveInEvent();
                _nativeRecorder.WaveFormat = waveFormat;
                _nativeRecorder.BufferMilliseconds = BufferMilliseconds;
                _nativeRecorder.DeviceNumber = selectedDevice;
                _nativeRecorder.NumberOfBuffers = 3;
            }
            catch
            {
                CleanupRecorder();
                throw;
            }
        }

        public void Start()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (CaptureState != CaptureState.Stopped) return;

            try
            {
                CaptureState = CaptureState.Starting;
                _nativeRecorder?.StartRecording();
                CaptureState = CaptureState.Capturing;
            }
            catch
            {
                CaptureState = CaptureState.Stopped;
                throw;
            }
        }

        public void Stop()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (CaptureState != CaptureState.Capturing) return;
            
            CaptureState = CaptureState.Stopping;
            _nativeRecorder?.StopRecording();
            
            while (CaptureState == CaptureState.Stopping)
            {
                Thread.Sleep(1); //Wait until stopped.
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CleanupRecorder()
        {
            if (_nativeRecorder == null) return;
            _nativeRecorder.RecordingStopped -= InvokeRecordingStopped;
            _nativeRecorder.DataAvailable -= InvokeDataAvailable;
            _nativeRecorder.Dispose();
            _nativeRecorder = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_nativeRecorder == null)
                throw new InvalidOperationException("You must initialize the recorder before calling starting!");
        }

        private void InvokeDataAvailable(object? sender, WaveInEventArgs e)
        {
            CaptureState = CaptureState.Capturing;
            OnDataAvailable?.Invoke(e.Buffer, e.BytesRecorded);
        }

        private void InvokeRecordingStopped(object? sender, StoppedEventArgs e)
        {
            CaptureState = CaptureState.Stopped;
            OnRecordingStopped?.Invoke(e.Exception);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                CleanupRecorder();
            }
            
            _disposed = true;
        }
    }
}