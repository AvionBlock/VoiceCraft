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
                if(value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }

        public int BitDepth
        {
            get
            {
                return Format switch
                {
                    AudioFormat.Pcm8 => 8,
                    AudioFormat.Pcm16 => 16,
                    AudioFormat.PcmFloat => 32,
                    _ => throw new ArgumentOutOfRangeException(nameof(Format))
                };
            }
        }

        public AudioFormat Format { get; set; }

        public int BufferMilliseconds
        {
            get => _bufferMilliseconds;
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice { get; set; }

        public CaptureState CaptureState { get; private set; }

        public event Action<byte[], int>? OnDataAvailable;
        public event Action<Exception?>? OnRecordingStopped;
        
        //Privates
        private readonly Lock _lockObj = new();
        private WaveInEvent? _nativeRecorder;
        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;
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
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();

                if (CaptureState != CaptureState.Stopped)
                    throw new InvalidOperationException(Locales.Locales.Audio_Recorder_InitFailed);

                //Cleanup previous recorder.
                CleanupRecorder();

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
                    _ => throw new NotSupportedException()
                };

                //Setup Recorder.
                _nativeRecorder = new WaveInEvent();
                _nativeRecorder.WaveFormat = waveFormat;
                _nativeRecorder.BufferMilliseconds = BufferMilliseconds;
                _nativeRecorder.DeviceNumber = selectedDevice;
                _nativeRecorder.NumberOfBuffers = 3;
                _nativeRecorder.RecordingStopped += InvokeRecordingStopped;
                _nativeRecorder.DataAvailable += InvokeDataAvailable;
            }
            catch
            {
                CleanupRecorder();
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Start()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (CaptureState != CaptureState.Stopped) return;

                CaptureState = CaptureState.Starting;
                _nativeRecorder?.StartRecording();
            }
            catch
            {
                CaptureState = CaptureState.Stopped;
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Stop()
        {
            _lockObj.Enter();
            
            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (CaptureState != CaptureState.Capturing) return;

                CaptureState = CaptureState.Stopping;
                _nativeRecorder?.StopRecording();
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Dispose()
        {
            _lockObj.Enter();
            
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            finally
            {
                _lockObj.Exit();
            }
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
                throw new InvalidOperationException(Locales.Locales.Audio_Recorder_Init);
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