using Android.Media;
using NAudio.Wave;
using System;
using System.Threading;

namespace VoiceCraft.Client.Android.Audio
{
    public class AndroidAudioPlayer : IWavePlayer
    {
        private readonly SynchronizationContext? _synchronizationContext;
        private volatile PlaybackState _playbackState;
        private IWaveProvider? _waveStream;
        private AudioTrack? _audioTrack;
        private float _volume;

        public int DesiredLatency { get; set; }

        public WaveFormat? OutputWaveFormat => _waveStream?.WaveFormat;
        public PlaybackState PlaybackState => _playbackState;
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _audioTrack?.SetVolume(value);
            }
        }

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AndroidAudioPlayer()
        {
            _synchronizationContext = SynchronizationContext.Current;
            _playbackState = PlaybackState.Stopped;
            DesiredLatency = 300;
        }

        public void Init(IWaveProvider waveProvider)
        {
            if (_playbackState != 0)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }

            if (_audioTrack != null)
            {
                _audioTrack.Stop();
                _audioTrack.Release();
                _audioTrack.Dispose();
                _audioTrack = null;
            }

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

            _waveStream = waveProvider;

            //Determine the channel mask
            ChannelOut channelMask = _waveStream.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(waveProvider))
            };

            //Determine the buffer size
            int minBufferSize = AudioTrack.GetMinBufferSize(_waveStream.WaveFormat.SampleRate, channelMask, encoding);
            int bufferSize = _waveStream.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)!
                    .SetContentType(AudioContentType.Music)!
                    .Build()!)
                .SetAudioFormat(new AudioFormat.Builder()!
                    .SetEncoding(encoding)!
                    .SetSampleRate(_waveStream.WaveFormat.SampleRate)!
                    .SetChannelMask(channelMask)!
                    .Build()!)
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .SetPerformanceMode(AudioTrackPerformanceMode.None)
                .Build();
            _audioTrack.SetVolume(Volume);
        }

        public void Play()
        {
            if (_audioTrack == null || _waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }

            if (_playbackState == PlaybackState.Stopped)
            {
                _playbackState = PlaybackState.Playing;
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
            else if (_playbackState == PlaybackState.Paused)
            {
                _playbackState = PlaybackState.Playing;
                _audioTrack?.Play();
            }
        }

        public void Stop()
        {
            if (_playbackState == PlaybackState.Stopped)
            {
                return;
            }

            _playbackState = PlaybackState.Stopped;
            _audioTrack?.Stop();
        }

        public void Pause()
        {
            if (_playbackState == PlaybackState.Stopped || _playbackState == PlaybackState.Paused)
            {
                return;
            }

            _playbackState = PlaybackState.Paused;
            _audioTrack?.Pause();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                _playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            if (_waveStream == null)
                throw new Exception("WaveStream was not found, could not initialize buffers!");

            int bufferSize = _waveStream.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            //Initialize the wave buffer
            WaveBuffer waveBuffer = new(bufferSize)
            {
                ByteBufferCount = bufferSize
            };

            while (PlaybackState != PlaybackState.Stopped)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //Fill the wave buffer with new samples
                int bytesRead = _waveStream.Read(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                if (bytesRead > 0)
                {
                    //Clear the unused space in the wave buffer if necessary
                    if (bytesRead < waveBuffer.ByteBufferCount)
                    {
                        waveBuffer.ByteBufferCount = (bytesRead + 3) & ~3;
                        Array.Clear(waveBuffer.ByteBuffer, bytesRead, waveBuffer.ByteBufferCount - bytesRead);
                    }

                    //Write the specified wave buffer to the audio track
                    if (_waveStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                    {
                        _audioTrack?.Write(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                    }
                    else if (_waveStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                    {
                        //AudioTrack.Write doesn't appreciate WaveBuffer.FloatBuffer
                        float[] floatBuffer = new float[waveBuffer.FloatBufferCount];
                        for (int i = 0; i < waveBuffer.FloatBufferCount; i++)
                        {
                            floatBuffer[i] = waveBuffer.FloatBuffer[i];
                        }
                        _audioTrack?.Write(floatBuffer, 0, floatBuffer.Length, WriteMode.Blocking);
                    }
                }
                else
                {
                    //Stop the audio track
                    _audioTrack?.Stop();
                    break;
                }
            }

            //Flush the audio track
            _audioTrack?.Flush();
        }

        protected virtual void RaisePlaybackStoppedEvent(Exception? exception = null)
        {
            //Raise the playback stopped event
            PlaybackStopped?.Invoke(this, new StoppedEventArgs(exception));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_playbackState != PlaybackState.Stopped)
                {
                    Stop();
                }
                _audioTrack?.Release();
                _audioTrack?.Dispose();
                _audioTrack = null;
            }
        }
    }
}