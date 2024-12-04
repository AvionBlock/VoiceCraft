using Android.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public sealed class AudioRecorder(AudioManager audioManager) : IAudioRecorder
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private string? _selectedDevice;
        private CaptureState _captureState = CaptureState.Stopped;
        private AudioRecord? _audioRecord;

        public WaveFormat WaveFormat { get; set; } = new(8000, 16, 1);
        public int BufferMilliseconds { get; set; } = 100;
        public AudioSource AudioSource { get; set; } = AudioSource.Mic;
        public bool IsRecording => _captureState == CaptureState.Capturing;
        public int? SessionId => _audioRecord?.AudioSessionId;

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler? RecordingStarted;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        ~AudioRecorder()
        {
            Dispose(false);
        }

        public void StartRecording()
        {
            //Starting capture procedure
            OpenRecorder();

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

            _captureState = CaptureState.Starting;
            var selectedDevice = audioManager.GetDevices(GetDevicesTargets.Inputs)?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == _selectedDevice);

            _audioRecord?.SetPreferredDevice(selectedDevice);
            _audioRecord?.StartRecording();
            ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
        }

        public void StopRecording()
        {
            if (_audioRecord == null)
            {
                return;
            }

            //Check if it has already been stopped
            if (_captureState == CaptureState.Stopped) return;
            _captureState = CaptureState.Stopped;
            CloseRecorder();
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
            _audioRecord?.Release();
            _audioRecord?.Dispose();
            _audioRecord = null;
        }

        private void OpenRecorder()
        {
            //We want to make sure the recorder is definitely closed.
            CloseRecorder();
            Encoding encoding;

            //Set the encoding
            if (WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
            {
                encoding = WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit or 32bit", nameof(WaveFormat))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE Float", nameof(WaveFormat));
            }

            //Set the channel type. Only accepts Mono or Stereo
            var channelMask = WaveFormat.Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(WaveFormat))
            };

            //Determine the buffer size
            var bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            //Determine min buffer size.
            var minBufferSize = AudioRecord.GetMinBufferSize(WaveFormat.SampleRate, channelMask, encoding);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }
            //Create the AudioRecord Object.
            _audioRecord = new AudioRecord(AudioSource, WaveFormat.SampleRate, channelMask, encoding, bufferSize);
        }

        private void CloseRecorder()
        {
            //Make sure that the recorder was opened
            if (_audioRecord is not { State: State.Initialized }) return;
            var audioRecord = _audioRecord;
            _audioRecord = null;
            audioRecord.Stop();
            audioRecord.Dispose();
        }

        private void RecordThread()
        {
            Exception? exception = null;
            try
            {
                RaiseRecordingStartedEvent();
                RecordingLogic();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                _captureState = CaptureState.Stopped;
                if(_audioRecord?.RecordingState != RecordState.Stopped)
                    _audioRecord?.Stop();

                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void RaiseRecordingStartedEvent()
        {
            var handler = RecordingStarted;
            if (handler == null) return;
            if (_synchronizationContext == null)
            {
                handler(this, EventArgs.Empty);
            }
            else
            {
                _synchronizationContext.Post(_ => handler(this, EventArgs.Empty), null);
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

            //Run the record loop
            while (_captureState != CaptureState.Stopped && _audioRecord != null)
            {
                if (_captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }

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
                            case < 0 when _audioRecord.RecordingState != RecordState.Recording:
                                throw new Exception("An error occured while trying to capture data.");
                        }
                        break;
                    }
                    case WaveFormatEncoding.IeeeFloat:
                    {
                        var floatBuffer = new float[bufferSize / 4];
                        var byteBuffer = new byte[bufferSize];
                        var floatsRead = _audioRecord.Read(floatBuffer, 0, floatBuffer.Length, 0);
                        Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                        switch (floatsRead)
                        {
                            case > 0:
                                DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, floatsRead * 4));
                                break;
                            case < 0 when _audioRecord.RecordingState != RecordState.Recording:
                                throw new Exception("An error occured while trying to capture data.");
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}