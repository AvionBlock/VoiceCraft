using Android.Media;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public sealed class AudioPlayer : IAudioPlayer
    {
        private readonly SynchronizationContext? _synchronizationContext;
        private string? _selectedDevice;
        private readonly AudioManager _audioManager;
        private IWaveProvider? _waveProvider;
        private AudioTrack? _audioTrack;
        private float _volume;

        public PlaybackState PlaybackState { get; private set; }
        public int? SessionId => _audioTrack?.AudioSessionId;

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
        public WaveFormat OutputWaveFormat { get; set; }

        public event EventHandler? PlaybackStarted;
        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AudioPlayer(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _synchronizationContext = SynchronizationContext.Current;

            _volume = 1.0f;
            PlaybackState = PlaybackState.Stopped;
            NumberOfBuffers = 2;
            DesiredLatency = 300;
            OutputWaveFormat = new WaveFormat();

            Usage = AudioUsageKind.Media;
            ContentType = AudioContentType.Music;

            Debug.WriteLine("Created!");
        }

        ~AudioPlayer()
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
            var channelMask = _waveProvider.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(waveProvider))
            };

            //Determine the buffer size
            var minBufferSize = AudioTrack.GetMinBufferSize(_waveProvider.WaveFormat.SampleRate, channelMask, encoding);
            var bufferSize = _waveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
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
                    .SetChannelMask(channelMask)
                    .Build()!)
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .Build();

            _audioTrack.SetVolume(Volume);

            var selectedDevice = _audioManager.GetDevices(GetDevicesTargets.Outputs)?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == _selectedDevice);
            _audioTrack.SetPreferredDevice(selectedDevice);
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
                ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
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

            if (PlaybackState is PlaybackState.Stopped or PlaybackState.Paused)
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

        public void SetDevice(string device)
        {
            _selectedDevice = device;
        }

        public void Dispose()
        {
            //Dispose of this object
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            _audioTrack?.Play();
            PlaybackState = PlaybackState.Playing;
        }

        private void ClosePlayer()
        {
            if (_audioTrack is not { State: AudioTrackState.Initialized }) return;
            var audioTrack = _audioTrack;
            _audioTrack = null;
            audioTrack.Stop();
            audioTrack.Dispose();
        }

        private void PlaybackThread()
        {
            Exception? exception = null;
            try
            {
                RaisePlaybackStartedEvent();
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
                if (_audioTrack?.PlayState != PlayState.Stopped)
                    _audioTrack?.Stop();

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
            var waveBufferSize = (_audioTrack.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * _waveProvider.WaveFormat.BlockAlign;
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
                var bytesRead = _waveProvider.Read(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                if (bytesRead > 0)
                {
                    //Clear the unused space in the wave buffer if necessary
                    if (bytesRead < waveBuffer.ByteBufferCount)
                    {
                        waveBuffer.ByteBufferCount = (bytesRead + 3) & ~3;
                        Array.Clear(waveBuffer.ByteBuffer, bytesRead, waveBuffer.ByteBufferCount - bytesRead);
                    }

                    switch (_waveProvider.WaveFormat.Encoding)
                    {
                        //Write the specified wave buffer to the audio track
                        case WaveFormatEncoding.Pcm:
                        {
                            var bytesWritten = _audioTrack.Write(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                            if (bytesWritten < 0 && (_audioTrack.PlayState != PlayState.Playing || _audioTrack.PlayState != PlayState.Paused))
                            {
                                throw new Exception("An error occurred while trying to write to the audio player.");
                            }

                            break;
                        }
                        case WaveFormatEncoding.IeeeFloat:
                        {
                            //AudioTrack.Write doesn't appreciate WaveBuffer.FloatBuffer
                            var floatBuffer = new float[waveBuffer.FloatBufferCount];
                            for (var i = 0; i < waveBuffer.FloatBufferCount; i++)
                            {
                                floatBuffer[i] = waveBuffer.FloatBuffer[i];
                            }
                            var floatsWritten = _audioTrack.Write(floatBuffer, 0, floatBuffer.Length, WriteMode.Blocking);

                            if (floatsWritten < 0 && (_audioTrack.PlayState != PlayState.Playing || _audioTrack.PlayState != PlayState.Paused))
                            {
                                throw new Exception("An error occurred while trying to write to the audio player.");
                            }

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    break;
                }

                _audioTrack.Flush();
            }
        }

        private void RaisePlaybackStartedEvent()
        {
            var handler = PlaybackStarted;
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

        private void RaisePlaybackStoppedEvent(Exception? e)
        {
            var handler = PlaybackStopped;
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

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (PlaybackState != PlaybackState.Stopped)
            {
                Stop();
            }
            _audioTrack?.Release();
            _audioTrack?.Dispose();
        }
    }
}