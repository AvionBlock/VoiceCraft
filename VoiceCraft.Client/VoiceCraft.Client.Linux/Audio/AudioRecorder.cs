using NAudio.Wave;
using System;
using System.Threading;
using NAudio.CoreAudioApi;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Linux.Audio
{
    public unsafe class AudioRecorder : IAudioRecorder
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private string? _selectedDevice;
        private CaptureState _captureState = CaptureState.Stopped;
        private ALCaptureDevice? _device;
        
        public WaveFormat WaveFormat { get; set; } = new(8000, 16, 1);
        public int BufferMilliseconds { get; set; } = 100;
        public bool IsRecording => _captureState == CaptureState.Capturing;
        
        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        ~AudioRecorder()
        {
            Dispose(false);
        }
        
        public void StartRecording()
        {
            //Check if we are already recording.
            if (_captureState == CaptureState.Capturing)
            {
                return;
            }
            
            //Make sure that we have some format to use.
            if (WaveFormat == null)
            {
                throw new ArgumentNullException(nameof(WaveFormat));
            }
            
            //Open Capture Device
            OpenCaptureDevice();

            if (_device == null) return;
            _captureState = CaptureState.Starting;
            ALC.CaptureStart((ALCaptureDevice)_device);
            ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
        }
        public void StopRecording()
        {
            if (_device == null)
                return;

            //Check if it has already been stopped
            if (_captureState == CaptureState.Stopped) return;
            _captureState = CaptureState.Stopped;
            CloseCaptureDevice();
        }

        public void SetDevice(string device)
        {
            _selectedDevice = device;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_captureState != CaptureState.Stopped)
            {
                StopRecording();
            }
        }
        
        private void OpenCaptureDevice()
        {
            CloseCaptureDevice();
            var format = (WaveFormat.BitsPerSample, WaveFormat.Channels) switch
            {
                (8, 1) => ALFormat.Mono8,
                (8, 2) => ALFormat.Stereo8,
                (16, 1) => ALFormat.Mono16,
                (16, 2) => ALFormat.Stereo16,
                _ => throw new NotSupportedException()
            };
            var bufferSize = BufferMilliseconds * WaveFormat.SampleRate / 1000;
            AL.GetError();
            _device = ALC.CaptureOpenDevice(_selectedDevice, WaveFormat.SampleRate, format, bufferSize * 2);
            if (_device == null)
            {
                throw new InvalidOperationException("Could not create device!");
            }
        }
        
        private void CloseCaptureDevice()
        {
            if (_device == null) return;
            var recorder = (ALCaptureDevice)_device;
            _device = null;
            ALC.CaptureStop(recorder);
            ALC.CaptureCloseDevice(recorder);
        }
        
        private void RecordThread()
        {
            Exception? exception = null;
            try
            {
                RecordingLogic();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                _captureState = CaptureState.Stopped;
                CloseCaptureDevice();

                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void RaiseRecordingStoppedEvent(Exception? e)
        {
            var handler = RecordingStopped;
            if (handler == null) return;
            if (_synchronizationContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                _synchronizationContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
            }
        }
        
        private void RecordingLogic()
        {
            //Initialize the wave buffer
            var bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            _captureState = CaptureState.Capturing;
            var capturedSamples = 0;
            var targetSamples = BufferMilliseconds * WaveFormat.SampleRate / 1000;

            //Run the record loop
            while (_captureState != CaptureState.Stopped && _device != null)
            {
                var device = (ALCaptureDevice)_device;
                if (_captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }
                // Query the number of captured samples
                ALC.GetInteger(device, AlcGetInteger.CaptureSamples, sizeof(int), &capturedSamples);

                if (capturedSamples < targetSamples) continue;
                var buffer = new byte[bufferSize];
                fixed (void* bufferPtr = buffer)
                    ALC.CaptureSamples(device, bufferPtr, targetSamples);
                
                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bufferSize));
            }
        }
    }
}