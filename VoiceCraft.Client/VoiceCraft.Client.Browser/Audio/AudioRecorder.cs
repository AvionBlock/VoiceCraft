using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VoiceCraft.Client.Browser.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        internal const string Lib = "openal";
        internal const CallingConvention AlcCallingConv = CallingConvention.Cdecl;
        [DllImport(Lib, EntryPoint = "alcOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)]
        public static extern ALDevice OpenDevice([In] string devicename);
        [DllImport(Lib, EntryPoint = "alcCaptureOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)]
        public static extern ALCaptureDevice CaptureOpenDevice(string devicename, uint frequency, ALFormat format, int buffersize);
        [DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)]
        public static unsafe extern void GetInteger(ALDevice device, AlcGetInteger param, int size, int* data);
        [DllImport(Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern void CaptureSamples(ALCaptureDevice device, IntPtr buffer, int samples);
        [DllImport(Lib, EntryPoint = "alcCaptureStart", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern void CaptureStart([In] ALCaptureDevice device);
        [DllImport(Lib, EntryPoint = "alcCaptureStop", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern void CaptureStop([In] ALCaptureDevice device);
        [DllImport(Lib, EntryPoint = "alcCaptureCloseDevice", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern bool CaptureCloseDevice([In] ALCaptureDevice device);

        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private ALCaptureDevice _device = ALCaptureDevice.Null;
        private bool _disposed;

        public WaveFormat WaveFormat { get; set; } = new(8000, 16, 1);
        public CaptureState CaptureState { get; private set; } = CaptureState.Stopped;
        public int BufferMilliseconds { get; set; } = 100;
        public string? SelectedDevice { get; set; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        ~AudioRecorder()
        {
            Dispose(false);
        }

        public void StartRecording()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if we are already recording or starting to record.
            if (CaptureState is CaptureState.Capturing or CaptureState.Starting) return;

            while (CaptureState is CaptureState.Stopping) //If stopping, wait.
                Task.Delay(1).GetAwaiter().GetResult();

            //Open Capture Device
            CaptureState = CaptureState.Starting;
            _device = OpenCaptureDevice(WaveFormat, BufferMilliseconds, SelectedDevice);
            ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
        }

        public void StopRecording()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_device == ALCaptureDevice.Null) return;

            //Check if it has already been stopped or is stopping.
            if (CaptureState is CaptureState.Stopped or CaptureState.Stopping) return;
            CaptureState = CaptureState.Stopping;

            //Block thread until it's fully stopped.
            while (CaptureState is CaptureState.Stopping)
                Task.Delay(1).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            if (CaptureState != CaptureState.Stopped)
            {
                StopRecording();
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioRecorder).ToString());
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
                CloseCaptureDevice(_device);
                _device = ALCaptureDevice.Null;
                CaptureState = CaptureState.Stopped;
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

        private unsafe void RecordingLogic()
        {
            //Initialize the wave buffer
            var bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            CaptureState = CaptureState.Capturing;
            var capturedSamples = 0;
            var targetSamples = BufferMilliseconds * WaveFormat.SampleRate / 1000;

            //Run the record loop
            while (CaptureState == CaptureState.Capturing && _device != ALCaptureDevice.Null)
            {
                // Query the number of captured samples
                ALC.GetInteger(_device, AlcGetInteger.CaptureSamples, sizeof(int), &capturedSamples);

                if (capturedSamples < targetSamples) continue;
                var buffer = new byte[bufferSize];
                fixed (void* bufferPtr = buffer)
                    ALC.CaptureSamples(_device, bufferPtr, targetSamples);

                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bufferSize));
            }
        }

        private static ALCaptureDevice OpenCaptureDevice(WaveFormat waveFormat, int bufferSizeMs, string? selectedDevice = null)
        {
            var format = (waveFormat.BitsPerSample, waveFormat.Channels) switch
            {
                (8, 1) => ALFormat.Mono8,
                (8, 2) => ALFormat.Stereo8,
                (16, 1) => ALFormat.Mono16,
                (16, 2) => ALFormat.Stereo16,
                _ => throw new NotSupportedException()
            };

            var bufferSize = bufferSizeMs * waveFormat.SampleRate / 1000; //Calculate buffer size IN SAMPLES!
            var device = ALC.CaptureOpenDevice(selectedDevice, waveFormat.SampleRate, format, bufferSize);
            if (device == ALCaptureDevice.Null)
            {
                throw new InvalidOperationException("Could not create device!");
            }

            ALC.CaptureStart(device);
            return device;
        }

        private static void CloseCaptureDevice(ALCaptureDevice device)
        {
            if (device == ALCaptureDevice.Null) return;
            ALC.CaptureStop(device);
            ALC.CaptureCloseDevice(device);
        }
    }
}
