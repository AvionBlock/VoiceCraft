using Android.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public sealed class AudioRecorder(AudioManager audioManager) : IAudioRecorder
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private AudioRecord? _audioRecord;
        private bool _disposed;

        public WaveFormat WaveFormat { get; set; } = new(8000, 16, 1);
        public CaptureState CaptureState { get; private set; } = CaptureState.Stopped;
        public int BufferMilliseconds { get; set; } = 100;
        public string? SelectedDevice { get; set; }
        public AudioSource AudioSource { get; set; }
        public int? SessionId => _audioRecord?.AudioSessionId;

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
            _audioRecord = OpenCaptureDevice(WaveFormat, BufferMilliseconds, audioManager, AudioSource, SelectedDevice);
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
                CloseCaptureDevice(_audioRecord);
                _audioRecord = null;
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
                        var bytesRead = _audioRecord.Read(byteBuffer, 0, bufferSize);
                        switch (bytesRead)
                        {
                            case > 0:
                                DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, bytesRead));
                                break;
                            case < 0:
                                throw new InvalidOperationException(Localizer.Get("Android.AudioRecorder.Exception.Capture"));
                        }

                        break;
                    }
                    case WaveFormatEncoding.IeeeFloat:
                    {
                        var floatBuffer = new float[bufferSize / sizeof(float)];
                        var floatsRead = _audioRecord.Read(floatBuffer, 0, floatBuffer.Length, 0);
                        switch (floatsRead)
                        {
                            case > 0:
                                var byteBuffer = new byte[bufferSize];
                                Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                                DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, floatsRead * sizeof(float)));
                                break;
                            case < 0:
                                throw new InvalidOperationException(Localizer.Get("Android.AudioRecorder.Exception.Capture"));
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static AudioRecord OpenCaptureDevice(WaveFormat waveFormat, int bufferSizeMs, AudioManager audioManager, AudioSource audioSource = AudioSource.Mic, string? selectedDevice = null)
        {
            //Set the encoding
            var encoding = (waveFormat.BitsPerSample, waveFormat.Encoding) switch
            {
                (8, WaveFormatEncoding.Pcm) => Encoding.Pcm8bit,
                (16, WaveFormatEncoding.Pcm) => Encoding.Pcm16bit,
                (32, WaveFormatEncoding.IeeeFloat) => Encoding.PcmFloat,
                _ => throw new NotSupportedException()
            };

            //Set the channel type. Only accepts Mono or Stereo
            var channelMask = waveFormat.Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new NotSupportedException()
            };

            //Determine the buffer size
            var bufferSize = bufferSizeMs * waveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % waveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % waveFormat.BlockAlign;
            }

            //Determine min buffer size.
            var minBufferSize = AudioRecord.GetMinBufferSize(waveFormat.SampleRate, channelMask, encoding);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            //Create the AudioRecord Object.
            var audioRecord = new AudioRecord(audioSource, waveFormat.SampleRate, channelMask, encoding, bufferSize);
            var device = audioManager.GetDevices(GetDevicesTargets.Inputs)
                ?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == selectedDevice);

            audioRecord.SetPreferredDevice(device);
            try
            {
                audioRecord.StartRecording();
            }
            catch
            {
                audioRecord.Dispose(); //Dispose audio recorder if created.
                throw; //Rethrow stack error.
            }
            return audioRecord;
        }
        
        private static void CloseCaptureDevice(AudioRecord? audioRecord)
        {
            //Make sure that the recorder was opened
            if (audioRecord is not { State: State.Initialized }) return;
            audioRecord.Stop();
            audioRecord.Dispose();
        }
    }
}