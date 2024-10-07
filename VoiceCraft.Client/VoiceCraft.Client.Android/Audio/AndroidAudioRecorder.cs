using Android.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Threading;

namespace VoiceCraft.Client.Android.Audio
{
    public class AndroidAudioRecorder : IWaveIn
    {
        private readonly AutoResetEvent callbackEvent;
        private readonly SynchronizationContext? synchronizationContext;
        private CaptureState captureState;
        private AudioRecord? audioRecord;

        public WaveFormat WaveFormat { get; set; }
        public int BufferMilliseconds { get; set; }
        public AudioSource audioSource { get; set; }

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AndroidAudioRecorder()
        {
            callbackEvent = new AutoResetEvent(false);
            synchronizationContext = SynchronizationContext.Current;
            audioSource = AudioSource.Mic;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            captureState = CaptureState.Stopped;
        }

        private void OpenRecorder()
        {
            //We want to make sure the recorder is definitely closed.
            CloseRecorder();
            Encoding encoding;
            ChannelIn channelMask;

            //Set the encoding
            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm || WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
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
            channelMask = WaveFormat.Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(WaveFormat))
            };

            //Determine the buffer size
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
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
            audioRecord = new AudioRecord(audioSource, WaveFormat.SampleRate, channelMask, encoding, bufferSize);
        }

        private void CloseRecorder()
        {
            //Make sure that the recorder was opened
            if (audioRecord != null && audioRecord.RecordingState != RecordState.Stopped)
            {
                audioRecord.Stop();
                audioRecord.Release();
                audioRecord.Dispose();
                audioRecord = null;
            }
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
                captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void RecordingLogic()
        {
            //Initialize the wave buffer
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            captureState = CaptureState.Capturing;

            //Run the record loop
            while (captureState != CaptureState.Stopped && audioRecord != null)
            {
                if (captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    byte[] byteBuffer = new byte[bufferSize];
                    var bytesRead = audioRecord.Read(byteBuffer, 0, bufferSize);
                    if (bytesRead > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, bytesRead));
                    }
                }
                else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    float[] floatBuffer = new float[bufferSize / 4];
                    byte[] byteBuffer = new byte[bufferSize];
                    var floatsRead = audioRecord.Read(floatBuffer, 0, floatBuffer.Length, 0);
                    Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                    if (floatsRead > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, floatsRead * 4));
                    }
                }
            }
        }

        private void RaiseRecordingStoppedEvent(Exception? e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (synchronizationContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    synchronizationContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        public void StartRecording()
        {
            //Starting capture procedure
            OpenRecorder();

            //Check if we are already recording.
            if (captureState == CaptureState.Capturing)
            {
                return;
            }

            //Make sure that we have some format to use.
            if (WaveFormat == null)
            {
                throw new ArgumentNullException(nameof(WaveFormat));
            }

            captureState = CaptureState.Starting;
            audioRecord?.StartRecording();
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        public void StopRecording()
        {
            if (audioRecord == null)
            {
                return;
            }

            //Check if it has already been stopped
            if (captureState != CaptureState.Stopped)
            {
                captureState = CaptureState.Stopped;
                CloseRecorder();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (captureState != CaptureState.Stopped)
                {
                    StopRecording();
                }
                audioRecord?.Release();
                audioRecord?.Dispose();
                audioRecord = null;
            }
        }
    }
}