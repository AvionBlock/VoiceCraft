using NAudio.Wave;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.EXT;
using System;
using System.Threading.Tasks;

namespace VoiceCraft.Client.Audio
{
    public unsafe class AudioCapture : Core.Audio.AudioCapture, IWaveIn
    {
        public WaveFormat WaveFormat { get; set; }
        public int BufferMilliseconds { get; set; }
        public string? SelectedDevice { get; set; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        private Device* _device;
        private readonly ALContext _alContext;
        private readonly Capture _captureContext;
        private readonly AL _al;
        private bool _isRecording = false;

        public AudioCapture()
        {
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;

            _alContext = ALContext.GetApi(true);
            _al = AL.GetApi(true);
            _alContext.TryGetExtension<Capture>(null, out _captureContext);
        }

        public void StartRecording()
        {
            ThrowIfDisposed();
            OpenCaptureDevice();

            // Clear Errors
            _alContext.GetError(_device);
            _isRecording = true;
            _captureContext.CaptureStart(_device);
            Task.Run(() => RecordingLogic());
        }

        public void StopRecording()
        {
            ThrowIfDisposed();

            // Clear Errors
            _alContext.GetError(_device);
            _isRecording = false; // Stop the recording loop
            _captureContext.CaptureStop(_device);
        }

        private void RecordingLogic()
        {
            var bufferSize = WaveFormat.ConvertLatencyToByteSize(BufferMilliseconds);
            int capturedSamples = 0;

            while (_isRecording)
            {
                // Query the number of captured samples
                _captureContext.GetContextProperty(_device, GetCaptureContextInteger.CaptureSamples, sizeof(int), &capturedSamples);

                if (capturedSamples >= bufferSize)
                {
                    byte[] buffer = new byte[bufferSize];
                    fixed (void* bufferPtr = buffer)
                        _captureContext.CaptureSamples(_device, bufferPtr, bufferSize); // Correct size in samples

                    // Check for errors after capturing samples
                    if (_alContext.GetError(_device) == ContextError.NoError)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bufferSize));
                    }
                    else
                    {
                        // Stop recording logic here, handle cleanup if necessary
                        StopRecording();
                        RecordingStopped?.Invoke(this, new StoppedEventArgs());
                    }
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void OpenCaptureDevice()
        {
            CloseCaptureDevice();
            BufferFormat format;
            switch (WaveFormat.BitsPerSample, WaveFormat.Channels)
            {
                case (8, 1):
                    format = BufferFormat.Mono8;
                    break;
                case (8, 2):
                    format = BufferFormat.Stereo8;
                    break;
                case (16, 1):
                    format = BufferFormat.Mono16;
                    break;
                case (16, 2):
                    format = BufferFormat.Stereo16;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var bufferSize = WaveFormat.ConvertLatencyToByteSize(BufferMilliseconds);
            _device = _captureContext.CaptureOpenDevice(SelectedDevice, (uint)WaveFormat.SampleRate, format, bufferSize);
            if (_device == null)
            {
                throw new AudioDeviceException("Could not create device!");
            }
        }

        private void CloseCaptureDevice()
        {
            if (_device == null) return;
            if (_isRecording)
                StopRecording();
            _captureContext.CaptureCloseDevice(_device);
            _device = null;
        }

        private void ThrowIfDisposed()
        {

        }
    }
}