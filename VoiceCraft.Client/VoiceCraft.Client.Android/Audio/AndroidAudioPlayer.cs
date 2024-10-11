using Android.Media;
using NAudio.Wave;
using System;
using System.Threading;

namespace VoiceCraft.Client.Android.Audio
{
    public class AndroidAudioPlayer : IWavePlayer
    {
        private readonly SynchronizationContext? _synchronizationContext;
        IWaveProvider? _waveProvider;
        AudioTrack? _audioTrack;
        float _volume;

        public PlaybackState PlaybackState { get; private set; }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = (value < 0.0f) ? 0.0f : (value > 1.0f) ? 1.0f : value;
                _audioTrack?.SetVolume(_volume);
            }
        }

        public int DesiredLatency { get; set; }

        public int NumberOfBuffers { get; set; }

        public AudioUsageKind Usage { get; set; }

        public AudioContentType ContentType { get; set; }

        public AudioTrackPerformanceMode PerformanceMode { get; set; }

        public WaveFormat OutputWaveFormat { get; set; }

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AndroidAudioPlayer()
        {
            _synchronizationContext = SynchronizationContext.Current;

            _volume = 1.0f;
            PlaybackState = PlaybackState.Stopped;
            NumberOfBuffers = 2;
            DesiredLatency = 300;
            OutputWaveFormat = new WaveFormat();

            Usage = AudioUsageKind.Media;
            ContentType = AudioContentType.Music;
            PerformanceMode = AudioTrackPerformanceMode.None;
        }

        ~AndroidAudioPlayer()
        {
            //Dispose of this object
            Dispose(false);
        }

        public void Init(IWaveProvider waveProvider)
        {
            if (PlaybackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }
            if (_audioTrack != null)
            {
                ClosePlayer();
            }

            //Initialize the wave provider
            Encoding encoding;
            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm || waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                encoding = waveProvider.WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit, or 32-bit", nameof(waveProvider))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE float", nameof(waveProvider));
            }
            _waveProvider = waveProvider;

            //Determine the channel mask
            ChannelOut channelMask = _waveProvider.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(waveProvider))
            };

            //Determine the buffer size
            int minBufferSize = AudioTrack.GetMinBufferSize(_waveProvider.WaveFormat.SampleRate, channelMask, encoding);
            int bufferSize = _waveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(Usage)!
                    .SetContentType(ContentType)!
                    .Build()!)
                .SetAudioFormat(new AudioFormat.Builder()
                    .SetEncoding(encoding)!
                    .SetSampleRate(_waveProvider.WaveFormat.SampleRate)!
                    .SetChannelMask(channelMask)!
                    .Build()!)
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .SetPerformanceMode(PerformanceMode)
                .Build();
            _audioTrack.SetVolume(Volume);
        }

        public void Play()
        {
            if (PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            if (_waveProvider == null || _audioTrack == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }

            //Start the wave player
            if (PlaybackState == PlaybackState.Stopped)
            {
                PlaybackState = PlaybackState.Playing;
                _audioTrack.Play();
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
            else if (PlaybackState == PlaybackState.Paused)
            {
                Resume();
            }
        }

        public void Pause()
        {
            if (_waveProvider == null || _audioTrack == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }

            if (PlaybackState == PlaybackState.Stopped || PlaybackState == PlaybackState.Paused)
            {
                return;
            }

            //Pause the wave player
            PlaybackState = PlaybackState.Paused;
            _audioTrack.Pause();
        }

        public void Stop()
        {
            if (_waveProvider == null || _audioTrack == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }

            if (PlaybackState == PlaybackState.Stopped)
            {
                return;
            }

            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;
            _audioTrack.Stop();
        }

        public void Dispose()
        {
            //Dispose of this object
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Resume()
        {
            if (PlaybackState == PlaybackState.Paused)
            {
                _audioTrack?.Play();
                PlaybackState = PlaybackState.Playing;
            }
        }

        private void ClosePlayer()
        {
            if (_audioTrack != null)
            {
                _audioTrack.Stop();
                _audioTrack.Release();
                _audioTrack.Dispose();
                _audioTrack = null;
            }
        }

        private void PlaybackThread()
        {
            Exception? exception = null;
            try
            {
                PlaybackLogic();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                PlaybackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            if (_waveProvider == null || _audioTrack == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }

            //Initialize the wave buffer
            int waveBufferSize = (_audioTrack.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * _waveProvider.WaveFormat.BlockAlign;
            waveBufferSize = (waveBufferSize + 3) & ~3;
            WaveBuffer waveBuffer = new(waveBufferSize)
            {
                ByteBufferCount = waveBufferSize
            };

            //Run the playback loop
            while (PlaybackState != PlaybackState.Stopped)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                //Fill the wave buffer with new samples
                int bytesRead = _waveProvider.Read(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                if (bytesRead > 0)
                {
                    //Clear the unused space in the wave buffer if necessary
                    if (bytesRead < waveBuffer.ByteBufferCount)
                    {
                        waveBuffer.ByteBufferCount = (bytesRead + 3) & ~3;
                        Array.Clear(waveBuffer.ByteBuffer, bytesRead, waveBuffer.ByteBufferCount - bytesRead);
                    }

                    //Write the specified wave buffer to the audio track
                    if (_waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                    {
                        _audioTrack.Write(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                    }
                    else if (_waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                    {
                        //AudioTrack.Write doesn't appreciate WaveBuffer.FloatBuffer
                        float[] floatBuffer = new float[waveBuffer.FloatBufferCount];
                        for (int i = 0; i < waveBuffer.FloatBufferCount; i++)
                        {
                            floatBuffer[i] = waveBuffer.FloatBuffer[i];
                        }
                        _audioTrack.Write(floatBuffer, 0, floatBuffer.Length, WriteMode.Blocking);
                    }
                }
                else
                {
                    //Stop the audio track
                    _audioTrack.Stop();
                    break;
                }
            }

            //Flush the audio track
            _audioTrack.Flush();
        }

        private void RaisePlaybackStoppedEvent(Exception? e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (_synchronizationContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    _synchronizationContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (PlaybackState != PlaybackState.Stopped)
                {
                    Stop();
                }
                _audioTrack?.Release();
                _audioTrack?.Dispose();
            }
        }
    }
}