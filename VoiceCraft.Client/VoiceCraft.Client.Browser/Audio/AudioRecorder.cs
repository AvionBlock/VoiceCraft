// using Microsoft.JSInterop;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Browser.Audio
{
    public sealed class AudioRecorder : IAudioRecorder
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private JSObject? _audioStream;
        private JSObject? _audioRecord;
        private bool _disposed;

        public WaveFormat WaveFormat { get; set; } = new(8000, 16, 1);
        public CaptureState CaptureState { get; private set; } = CaptureState.Stopped;
        public int BufferMilliseconds { get; set; } = 100;
        public string? SelectedDevice { get; set; }
        // public int? SessionId => _audioRecord?.AudioSessionId;

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
                Thread.Sleep(1);

            //Open Capture Device
            CaptureState = CaptureState.Starting;
            (_audioRecord, _audioStream) = OpenCaptureDevice(WaveFormat, BufferMilliseconds, SelectedDevice);
            ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
        }

        public void StopRecording()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            //Check if device is already closed/null.
            if (_audioRecord == null) return;

            //Check if it has already been stopped or is stopping.
            if (CaptureState is CaptureState.Stopped or CaptureState.Stopping) return;
            CaptureState = CaptureState.Stopping;
            
            //Block thread until it's fully stopped.
            while(CaptureState is CaptureState.Stopping)
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
                CloseCaptureDevice(_audioRecord, _audioStream);
                _audioRecord = null;
                _audioStream = null;
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

        private void RecordingLogic()
        {
            //Initialize the wave buffer
            var bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            CaptureState = CaptureState.Capturing;

            //Run the record loop
            while (CaptureState == CaptureState.Capturing && _audioRecord != null)
            {
                switch (WaveFormat.Encoding)
                {
                    case WaveFormatEncoding.Pcm:
                    {
                        var byteBuffer = new byte[bufferSize];
                        // var bytesRead = _audioRecord.Read(byteBuffer, 0, bufferSize);
                        // switch (bytesRead)
                        // {
                        //     case > 0:
                        //         DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, bytesRead));
                        //         break;
                        //     case < 0:
                        //         throw new InvalidOperationException(Locales.Locales.Android_AudioRecorder_Exception_Capture);
                        // }

                        break;
                    }
                    case WaveFormatEncoding.IeeeFloat:
                    {
                        var floatBuffer = new float[bufferSize / sizeof(float)];
                        // var floatsRead = _audioRecord.Read(floatBuffer, 0, floatBuffer.Length, 0);
                        // switch (floatsRead)
                        // {
                        //     case > 0:
                        //         var byteBuffer = new byte[bufferSize];
                        //         Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                        //         DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, floatsRead * sizeof(float)));
                        //         break;
                        //     case < 0:
                        //         throw new InvalidOperationException(Locales.Locales.Android_AudioRecorder_Exception_Capture);
                        // }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static (JSObject, JSObject) OpenCaptureDevice(WaveFormat waveFormat, int bufferSizeMs, string? selectedDevice = null)
        {
            // throw new Exception("a problem");
            //Set the encoding
            // var encoding = (waveFormat.BitsPerSample, waveFormat.Encoding) switch
            // {
            //     (8, WaveFormatEncoding.Pcm) => Encoding.Pcm8bit,
            //     (16, WaveFormatEncoding.Pcm) => Encoding.Pcm16bit,
            //     (32, WaveFormatEncoding.IeeeFloat) => Encoding.PcmFloat,
            //     _ => throw new NotSupportedException()
            // };

            //Set the channel type. Only accepts Mono or Stereo
            // var channelMask = waveFormat.Channels switch
            // {
            //     1 => ChannelIn.Mono,
            //     2 => ChannelIn.Stereo,
            //     _ => throw new NotSupportedException()
            // };

            //Determine the buffer size
            var bufferSize = bufferSizeMs * waveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % waveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % waveFormat.BlockAlign;
            }

            JSObject audioStream = EmbedInteropRecord.construct();
            EmbedInteropRecord.applyTo(audioStream, "default", waveFormat.Channels, waveFormat.SampleRate, bufferSize);
            JSObject audioRecord = EmbedInteropRecord.constructRec(audioStream);
            // TODO
            // var device = audioManager.GetDevices(GetDevicesTargets.Inputs)
            //     ?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == selectedDevice);
            //
            // audioRecord.SetPreferredDevice(device);
            try
            {
                EmbedInteropRecord.startRec(audioRecord);
            }
            catch
            {
                CloseCaptureDevice(audioRecord, audioStream);
                throw; //Rethrow stack error.
            }
            return (audioRecord, audioStream);
        }
        
        private static void CloseCaptureDevice(JSObject? audioRecord, JSObject? audioStream)
        {
            //Make sure that the recorder was opened
            if (audioRecord == null || audioStream == null) return;
            EmbedInteropRecord.deconstruct(audioRecord, audioStream);
        }
    }

    static partial class EmbedInteropRecord
    {
        [JSImport("constructStream", "audio_recorder.js")]
        public static partial JSObject construct();

        [JSImport("constructRec", "audio_recorder.js")]
        public static partial JSObject constructRec(JSObject mediaStream);

        [JSImport("startRec", "audio_recorder.js")]
        public static partial void startRec(JSObject mediaStream);

        [JSImport("applyTo", "audio_recorder.js")]
        public static partial void applyTo(JSObject audioStream, string deviceId, int numOfChannels, int sampleRate, int bufferSize);

        [JSImport("deconstruct", "audio_recorder.js")]
        public static partial void deconstruct(JSObject mediaRecorder, JSObject audioRecord);

        // IMPORTANT: This gives devices in ["name", "id", etc...]
        [JSImport("getDevices", "audio_recorder.js")]
        public static partial string[] getDevices();
    }
}
