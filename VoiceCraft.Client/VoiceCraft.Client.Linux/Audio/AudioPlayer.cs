using System;
using System.Linq;
using System.Threading;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Linux.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private const int NumberOfBuffers = 3;

        //Public Properties
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Sample rate must be greater than or equal to zero!");

                _sampleRate = value;
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }

        public int BitDepth
        {
            get
            {
                return Format switch
                {
                    AudioFormat.Pcm8 => 8,
                    AudioFormat.Pcm16 => 16,
                    AudioFormat.PcmFloat => 32,
                    _ => throw new ArgumentOutOfRangeException(nameof(Format))
                };
            }
        }

        public AudioFormat Format { get; set; }

        public int BufferMilliseconds
        {
            get => _bufferMilliseconds;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice { get; set; }

        public PlaybackState PlaybackState { get; private set; }

        public event Action<Exception?>? OnPlaybackStopped;

        private readonly Lock _lockObj = new();
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private Func<byte[], int, int, int>? _playerCallback;
        private ALDevice _nativePlayer;
        private ALContext _nativePlayerContext;
        private ALFormat _alFormat;
        private int _bufferSamples;
        private int _bufferBytes;
        private int _blockAlign;
        private int _source;
        private AudioBuffer[] _buffers = [];
        private bool _disposed;

        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;

        public AudioPlayer(int sampleRate, int channels, AudioFormat format)
        {
            SampleRate = sampleRate;
            Channels = channels;
            Format = format;
        }

        ~AudioPlayer()
        {
            //Dispose of this object.
            Dispose(false);
        }

        public void Initialize(Func<byte[], int, int, int> playerCallback)
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();

                //Check if already playing.
                if (PlaybackState != PlaybackState.Stopped)
                    throw new InvalidOperationException("Cannot initialize when playing!");

                //Cleanup previous player.
                CleanupPlayer();

                _playerCallback = playerCallback;
                //Check if the format is supported first.
                _alFormat = (Format, Channels) switch
                {
                    (AudioFormat.Pcm8, 1) => ALFormat.Mono8,
                    (AudioFormat.Pcm8, 2) => ALFormat.Stereo8,
                    (AudioFormat.Pcm16, 1) => ALFormat.Mono16,
                    (AudioFormat.Pcm16, 2) => ALFormat.Stereo16,
                    (AudioFormat.PcmFloat, 1) => ALFormat.MonoFloat32Ext,
                    (AudioFormat.PcmFloat, 2) => ALFormat.StereoFloat32Ext,
                    _ => throw new NotSupportedException()
                };
                
                _bufferSamples = (BufferMilliseconds + NumberOfBuffers - 1) / NumberOfBuffers * (SampleRate / 1000); //Calculate buffer size IN SAMPLES!
                _bufferBytes = BitDepth / 8 * Channels * _bufferSamples;
                _blockAlign = Channels * (BitDepth / 8);
                if (_bufferBytes % _blockAlign != 0)
                {
                    _bufferBytes = _bufferBytes + _blockAlign - _bufferBytes % _blockAlign;
                }
                
                //Open and setup device.
                _nativePlayer = ALC.OpenDevice(SelectedDevice);
                if (_nativePlayer == ALDevice.Null)
                    throw new InvalidOperationException($"Failed to open device {SelectedDevice ?? "(default device)"}!");

                _nativePlayerContext = ALC.CreateContext(_nativePlayer, (int[]?)null);
                if (_nativePlayerContext == ALContext.Null)
                    throw new InvalidOperationException("Failed to create device context!");
                
                ALC.MakeContextCurrent(_nativePlayerContext);
                ALC.ProcessContext(_nativePlayerContext);

                //Generate Source
                _source = AL.GenSource();
                AL.Source(_source, ALSourcef.Pitch, 1);
                AL.Source(_source, ALSourcef.Gain, 1.0f);
                AL.Source(_source, ALSourceb.Looping, false);
                AL.Source(_source, ALSource3f.Position, 0, 0, 0);
                AL.Source(_source, ALSource3f.Velocity, 0, 0, 0);
                
                //Generate Buffers
                var buffers = AL.GenBuffers(NumberOfBuffers);

                _buffers = new AudioBuffer[NumberOfBuffers];
                for (var i = 0; i < NumberOfBuffers; i++)
                    _buffers[i] = new AudioBuffer(buffers[i], _bufferBytes, SampleRate, _alFormat);
            }
            catch
            {
                CleanupPlayer();
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Play()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();

                //Resume or start playback.
                switch (PlaybackState)
                {
                    case PlaybackState.Stopped:
                        PlaybackState = PlaybackState.Starting;
                        ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
                        break;
                    case PlaybackState.Paused:
                        Resume();
                        break;
                    case PlaybackState.Starting:
                    case PlaybackState.Playing:
                    case PlaybackState.Stopping:
                    default:
                        break;
                }
            }
            catch
            {
                PlaybackState = PlaybackState.Stopped;
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Pause()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (PlaybackState != PlaybackState.Playing) return;

                AL.SourcePause(_source);
                PlaybackState = PlaybackState.Paused;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Stop()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                
                if (PlaybackState != PlaybackState.Playing) return;

                PlaybackState = PlaybackState.Stopping;
                AL.SourceStop(_source);
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Dispose()
        {
            _lockObj.Enter();

            try
            {
                //Dispose of this object
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        private void CleanupPlayer()
        {
            _playerCallback = null;
            //Cleanup the source.
            if (_source != 0)
            {
                AL.SourceStop(_source);
                AL.DeleteSource(_source);
                _source = 0;
            }

            //Cleanup the buffers.
            if (_buffers.Length != 0)
            {
                foreach (var buffer in _buffers)
                    buffer.Delete();
                _buffers = [];
            }

            //Destroy the context.
            if (_nativePlayerContext != ALContext.Null)
            {
                ALC.MakeContextCurrent(_nativePlayerContext);
                ALC.DestroyContext(_nativePlayerContext);
                _nativePlayerContext = ALContext.Null;
            }

            //Destroy the device.
            if (_nativePlayer == ALDevice.Null) return;
            ALC.CloseDevice(_nativePlayer);
            _nativePlayer = ALDevice.Null;
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if (_nativePlayer == ALDevice.Null || _nativePlayerContext == ALContext.Null || _playerCallback == null)
                throw new InvalidOperationException("Audio player is not intialized!");
        }

        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            AL.SourcePlay(_source);
            PlaybackState = PlaybackState.Playing;
        }

        private void InvokePlaybackStopped(Exception? exception = null)
        {
            PlaybackState = PlaybackState.Stopped;
            var handler = OnPlaybackStopped;
            if (handler == null) return;
            if (_synchronizationContext == null)
            {
                handler(exception);
            }
            else
            {
                _synchronizationContext.Post(_ => handler(exception), null);
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
                if ((ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState) != ALSourceState.Stopped)
                    AL.SourceStop(_source);
                InvokePlaybackStopped(exception);
            }
        }

        private void Dispose(bool _)
        {
            if (_disposed) return;

            //Unmanaged resource. cleanup is necessary.
            CleanupPlayer();

            _disposed = true;
        }

        private void PlaybackLogic()
        {
            //This shouldn't happen...
            if (_playerCallback == null)
                throw new InvalidOperationException("Player callback was not found!");

            //Fill Buffers.
            foreach (var buffer in _buffers)
            {
                var read = _playerCallback(buffer.Data, 0, _bufferBytes);
                if (read > 0)
                    buffer.FillBuffer(read);

                buffer.SourceQueue(_source);
            }

            AL.SourcePlay(_source);
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped)
            {
                var state = State(); //This can sometimes return 0.
                if (state is ALSourceState.Stopped or 0)
                    break; //Reached the end of the playback buffer or the audio player has stopped.

                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //Get all buffers that have been processed.
                var processedBuffers = AL.GetSource(_source, ALGetSourcei.BuffersProcessed);
                if (processedBuffers <= 0) continue;

                //Unqueue the processed buffers.
                var buffers = AL.SourceUnqueueBuffers(_source, processedBuffers);
                foreach (var buffer in buffers)
                {
                    //Get the buffer corresponding to the ID.
                    var audioBuffer = _buffers.First(x => x.Id == buffer);
                    audioBuffer.Clear();
                    //Fill buffers with more data
                    var read = _playerCallback(audioBuffer.Data, 0, _bufferBytes);
                    if (read > 0)
                        audioBuffer.FillBuffer(read);

                    audioBuffer.SourceQueue(_source);
                }
            }

            return;

            ALSourceState State() => (ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState);
        }

        private class AudioBuffer(int id, int size, int sampleRate, ALFormat format)
        {
            public int Id { get; } = id;
            public byte[] Data { get; } = GC.AllocateArray<byte>(size, true);

            public void Clear()
            {
                Array.Clear(Data, 0, Data.Length);
            }

            public unsafe void FillBuffer(int read)
            {
                fixed (byte* byteBufferPtr = Data)
                    AL.BufferData(Id, format, byteBufferPtr, read, sampleRate);
            }

            public void SourceQueue(int sourceId)
            {
                AL.SourceQueueBuffer(sourceId, Id);
            }

            public void Delete()
            {
                AL.DeleteBuffer(Id);
            }
        }
    }
}