using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Core;

// using Microsoft.NET.WebAssembly.Threading;

namespace VoiceCraft.Client.Browser.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private IWaveProvider? _waveProvider;
        private JSObject? _audioInstance;
        private JSObject? _audioBuffer;
        private JSObject[] _buffers = [];
        private float _volume = 1.0f;
        private bool _disposed;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                // _audioTrack?.SetVolume(_volume);
            }
        }

        public int DesiredLatency { get; set; } = 100;
        public string? SelectedDevice { get; set; }
        public int NumberOfBuffers { get; set; } = 2;
        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;
        public WaveFormat OutputWaveFormat { get; private set; } = new();

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
                throw new InvalidOperationException("Can't re-initialize during playback");

            //Close previous audio track if it's not closed.
            if (_audioInstance != null)
                CloseDevice();

            //Create/Open new audio track.
            var (audioInstance, audioBuffer) = CreateAudioTrack(waveProvider, DesiredLatency, SelectedDevice);
            _waveProvider = waveProvider;
            _audioInstance = audioInstance;
            _audioBuffer = audioBuffer;
            // _buffers = GenerateBuffers(NumberOfBuffers);
            // _audioTrack.SetVolume(_volume); //Force set volume.

            //Set output wave format.
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            EmbedInteropPlayer.msg("we are start");
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            // if (_audioInstance == null || _waveProvider == null)
            //     throw new InvalidOperationException(Locales.Locales.Android_AudioPlayer_Exception_Init);

            //Resume or start playback.
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    // throw new InvalidOperationException("We are having fun");
                    ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
                    break;
                case PlaybackState.Paused:
                    Resume();
                    break;
                case PlaybackState.Playing:
                default:
                    break;
            }
            EmbedInteropPlayer.msg("This is cool!");
        }

        public void Stop()
        {
            EmbedInteropPlayer.msg("We are stop");
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_audioInstance == null || _waveProvider == null)
                throw new InvalidOperationException("not instanciated");

            //Check if it has already been stopped.
            if (PlaybackState == PlaybackState.Stopped) return;

            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;

            //Block thread until it's fully stopped.
            while (_audioInstance != null)
                Task.Delay(1).GetAwaiter().GetResult();
            EmbedInteropPlayer.msg("This is is end!");
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_audioInstance == null || _waveProvider == null)
                throw new InvalidOperationException("not instanciated");

            //Check if it has already been paused or is not playing.
            if (PlaybackState != PlaybackState.Playing) return;

            //Pause the wave player.
            if (_audioInstance != null) {
                EmbedInteropPlayer.suspend(_audioInstance);
            }
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
            if (_audioInstance != null) {
                EmbedInteropPlayer.resume(_audioInstance);
            }
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
                if (_audioInstance != null)
                    CloseDevice();

                PlaybackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            EmbedInteropPlayer.msg("Inside");
            if (_waveProvider == null || _audioInstance == null)
                throw new InvalidOperationException("not instanciated");
            
            //Calculate buffer size.
            // var waveBufferSize = (_audioInstance.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * _waveProvider.WaveFormat.BlockAlign;
            // waveBufferSize = (waveBufferSize + 3) & ~3;
            //
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped && _audioInstance != null)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (EmbedInteropPlayer.isStopped(_audioInstance))
                    break;

                //Fill the wave buffer with new samples
                // var byteBuffer = GC.AllocateArray<byte>(waveBufferSize, true);
                // var read = _waveProvider.Read(byteBuffer, 0, waveBufferSize);
                // if (read <= 0) break;
                switch (_waveProvider.WaveFormat.Encoding)
                {
                    //Write the specified wave buffer to the audio track
                    case WaveFormatEncoding.Pcm:
                    {
                        EmbedInteropPlayer.decode(_audioBuffer);
                        // var bytesWritten = _audioInstance.Write(byteBuffer, 0, read);
                        // if (bytesWritten < 0 && _audioInstance.PlayState is not (PlayState.Playing or PlayState.Paused))
                        // {
                        //     throw new Exception(Locales.Locales.Android_AudioPlayer_Exception_Write);
                        // }

                        break;
                    }
                    case WaveFormatEncoding.IeeeFloat:
                    {
                        EmbedInteropPlayer.decode(_audioBuffer);
                        // var floatBuffer = new float[waveBufferSize / sizeof(float)];
                        // Buffer.BlockCopy(byteBuffer, 0, floatBuffer, 0, read);
                        // var floatsWritten = _audioInstance.Write(floatBuffer, 0, read / sizeof(float), WriteMode.Blocking);
                        // if (floatsWritten < 0 && _audioInstance.PlayState is not (PlayState.Playing or PlayState.Paused))
                        // {
                        //     throw new Exception(Locales.Locales.Android_AudioPlayer_Exception_Write);
                        // }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            //     _audioInstance.Flush();
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
            if (_audioInstance != null)
                CloseDevice();

            _disposed = true;
        }

        private static (JSObject, JSObject) CreateAudioTrack(IWaveProvider waveProvider, int desiredLatency, string? deviceName)
        {
            //Initialize the wave provider
            // Encoding encoding;
            if (waveProvider.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
            {
                // encoding = waveProvider.WaveFormat.BitsPerSample switch
                switch (waveProvider.WaveFormat.BitsPerSample)
                {
                    // 8 => Encoding.Pcm8bit,
                    // 16 => Encoding.Pcm16bit,
                    // 32 => Encoding.PcmFloat;
                    case 32: break;
                    default:
                        throw new ArgumentException("Unsupported output for: ", nameof(waveProvider));
                }
            }
            else
            {
                throw new ArgumentException("Unsupported format for: ", nameof(waveProvider));
            }

            JSObject audioTrack = EmbedInteropPlayer.construct();
            var bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(desiredLatency);
            JSObject audioBuffer = EmbedInteropPlayer.getBuffer(audioTrack, waveProvider.WaveFormat.Channels, bufferSize * waveProvider.WaveFormat.Channels, waveProvider.WaveFormat.SampleRate);

            // var selectedDevice = audioManager.GetDevices(GetDevicesTargets.Outputs)
            //     ?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == deviceName);
            // audioTrack.SetPreferredDevice(selectedDevice);

            return (audioTrack, audioBuffer);
        }
        
        private void CloseDevice()
        {
            // if (_audioInstance is not { State: AudioTrackState.Initialized }) return;
            if (_audioInstance == null) return;
            // EmbedInterop.stop(_audioInstance).GetAwaiter().GetResult();
            EmbedInteropPlayer.stop(_audioInstance);
            // _audioInstance.DisposeAsync();
            _audioInstance = null;

            if (_buffers.Length != 0)
            {
                _buffers = [];
            }
        }
    }

    internal static partial class EmbedInteropPlayer
    {
        [JSImport("constructAudioContext", "audio_player.js")]
        public static partial JSObject construct();
        // public static partial void construct(JSObject options);

        [JSImport("stopAudioContext", "audio_player.js")]
        public static partial void stop(JSObject audioInstance);

        [JSImport("isStoppedAudioContext", "audio_player.js")]
        public static partial bool isStopped(JSObject audioInstance);

        [JSImport("resumeAudioContext", "audio_player.js")]
        public static partial void resume(JSObject audioInstance);

        [JSImport("suspendAudioContext", "audio_player.js")]
        public static partial void suspend(JSObject audioInstance);

        [JSImport("getBuffers", "audio_player.js")]
        public static partial JSObject getBuffer(JSObject audioInstance, int channels, int length, int sampleRate);

        [JSImport("decodeAudioData", "audio_player.js")]
        public static partial void decode(JSObject audioBuffer);

        [JSImport("msg", "proc.js")]
        public static partial void msg(string msg);
    }
}
