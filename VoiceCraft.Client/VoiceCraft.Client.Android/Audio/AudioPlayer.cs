using Android.Media;
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
    public class AudioPlayer(AudioManager audioManager) : IAudioPlayer
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private IWaveProvider? _waveProvider;
        private AudioTrack? _audioTrack;
        private float _volume = 1.0f;
        private bool _disposed;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                _audioTrack?.SetVolume(_volume);
            }
        }

        public int DesiredLatency { get; set; } = 100;
        public string? SelectedDevice { get; set; }
        public int NumberOfBuffers { get; set; } = 2;
        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;
        public WaveFormat OutputWaveFormat { get; private set; } = new();
        public AudioUsageKind Usage { get; set; } = AudioUsageKind.Media;
        public AudioContentType ContentType { get; set; } = AudioContentType.Music;

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        ~AudioPlayer()
        {
            //Dispose of this object
            Dispose(false);
        }

        public void Init(IWaveProvider waveProvider)
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if already playing.
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.Reinit"));

            //Close previous audio track if it's not closed.
            if (_audioTrack != null)
                CloseDevice();

            //Create/Open new audio track.
            var audioTrack = CreateAudioTrack(audioManager, waveProvider, DesiredLatency, Usage, ContentType,
                SelectedDevice);
            _waveProvider = waveProvider;
            _audioTrack = audioTrack;
            _audioTrack.SetVolume(_volume); //Force set volume.

            //Set output wave format.
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_audioTrack == null || _waveProvider == null)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.Init"));

            //Resume or start playback.
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
                    break;
                case PlaybackState.Paused:
                    Resume();
                    break;
                case PlaybackState.Playing:
                default:
                    break;
            }
        }

        public void Stop()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_audioTrack == null || _waveProvider == null)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.Init"));

            //Check if it has already been stopped.
            if (PlaybackState == PlaybackState.Stopped) return;

            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;

            //Block thread until it's fully stopped.
            while (_audioTrack != null)
                Task.Delay(1).GetAwaiter().GetResult();
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_audioTrack == null || _waveProvider == null)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.Init"));

            //Check if it has already been paused or is not playing.
            if (PlaybackState != PlaybackState.Playing) return;

            //Pause the wave player.
            _audioTrack?.Pause();
            PlaybackState = PlaybackState.Paused;
        }

        public void Dispose()
        {
            //Dispose of this object
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            _audioTrack?.Play();
            PlaybackState = PlaybackState.Playing;
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
                if (_audioTrack != null)
                    CloseDevice();

                PlaybackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            if (_waveProvider == null || _audioTrack == null)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.Init"));
            
            //Calculate buffer size.
            var waveBufferSize = (_audioTrack.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * _waveProvider.WaveFormat.BlockAlign;
            waveBufferSize = (waveBufferSize + 3) & ~3;

            //Run the playback loop
            _audioTrack.Play();
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped && _audioTrack != null)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (_audioTrack.PlayState == PlayState.Stopped)
                    break;

                //Fill the wave buffer with new samples
                var byteBuffer = GC.AllocateArray<byte>(waveBufferSize, true);
                var read = _waveProvider.Read(byteBuffer, 0, waveBufferSize);
                if (read <= 0) break;
                switch (_waveProvider.WaveFormat.Encoding)
                {
                    //Write the specified wave buffer to the audio track
                    case WaveFormatEncoding.Pcm:
                    {
                        var bytesWritten = _audioTrack.Write(byteBuffer, 0, read);
                        if (bytesWritten < 0 && _audioTrack.PlayState is not (PlayState.Playing or PlayState.Paused))
                        {
                            throw new Exception(Localizer.Get("Android.AudioPlayer.Exception.Write"));
                        }

                        break;
                    }
                    case WaveFormatEncoding.IeeeFloat:
                    {
                        var floatBuffer = new float[waveBufferSize / sizeof(float)];
                        Buffer.BlockCopy(byteBuffer, 0, floatBuffer, 0, read);
                        var floatsWritten = _audioTrack.Write(floatBuffer, 0, read / sizeof(float), WriteMode.Blocking);
                        if (floatsWritten < 0 && _audioTrack.PlayState is not (PlayState.Playing or PlayState.Paused))
                        {
                            throw new Exception(Localizer.Get("Android.AudioPlayer.Exception.Write"));
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _audioTrack.Flush();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;
            if (PlaybackState != PlaybackState.Stopped)
            {
                Stop();
            }
            
            //Close previous audio track if it was not closed by the player thread.
            if (_audioTrack != null)
                CloseDevice();

            _disposed = true;
        }

        private static AudioTrack CreateAudioTrack(AudioManager audioManager, IWaveProvider waveProvider,
            int desiredLatency, AudioUsageKind usage, AudioContentType content, string? deviceName)
        {
            //Initialize the wave provider
            Encoding encoding;
            if (waveProvider.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
            {
                encoding = waveProvider.WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException(Localizer.Get("Android.AudioPlayer.Exception.Encoding"),
                        nameof(waveProvider))
                };
            }
            else
            {
                throw new ArgumentException(Localizer.Get("Android.AudioPlayer.Exception.Format"), nameof(waveProvider));
            }

            //Determine the channel mask
            var channelMask = waveProvider.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException(Localizer.Get("Android.AudioPlayer.Exception.ChannelMask"), nameof(waveProvider))
            };

            //Determine the buffer size
            var minBufferSize = AudioTrack.GetMinBufferSize(waveProvider.WaveFormat.SampleRate, channelMask, encoding);
            var bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(desiredLatency);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            var audioAttributes = new AudioAttributes.Builder().SetUsage(usage)?.SetContentType(content)?.Build();
            var audioFormat = new AudioFormat.Builder().SetEncoding(encoding)
                ?.SetSampleRate(waveProvider.WaveFormat.SampleRate)?.SetChannelMask(channelMask).Build();

            if (audioAttributes == null || audioFormat == null)
                throw new InvalidOperationException(Localizer.Get("Android.AudioPlayer.Exception.CreateAudioTrack"));

            var audioTrack = new AudioTrack.Builder().SetAudioAttributes(audioAttributes).SetAudioFormat(audioFormat)
                .SetBufferSizeInBytes(bufferSize).SetTransferMode(AudioTrackMode.Stream).Build();
            audioTrack.SetVolume(1.0f);

            var selectedDevice = audioManager.GetDevices(GetDevicesTargets.Outputs)
                ?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == deviceName);
            audioTrack.SetPreferredDevice(selectedDevice);

            return audioTrack;
        }
        
        private void CloseDevice()
        {
            if (_audioTrack is not { State: AudioTrackState.Initialized }) return;
            _audioTrack.Stop();
            _audioTrack.Dispose();
            _audioTrack = null;
        }
    }
}