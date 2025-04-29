using System;
using System.Linq;
using System.Threading;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

using System.Runtime.InteropServices;

namespace VoiceCraft.Client.Browser.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        internal const string Lib = "openal";
        internal const CallingConvention ALCallingConvention = CallingConvention.Cdecl;
        internal const CallingConvention AlcCallingConv = CallingConvention.Cdecl;
                    // AL.BufferData(buffer, _format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
        [DllImport(Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void BufferData(int bid, ALFormat format, IntPtr buffer, int size, int freq);
                    // AL.DeleteBuffers(_buffers);
        [DllImport(Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void DeleteBuffers(int n, [In] int* buffers);
                    // AL.DeleteSource(_source);
        [DllImport(Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void DeleteSources(int n, [In] int* sources);
                    // AL.GenBuffers(numberOfBuffers);
        [DllImport(Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void GenBuffers(int n, [Out] int* buffers);
                    // AL.GenSource();
        [DllImport(Lib, EntryPoint = "alGenSources", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void GenSources(int n, [In] int* sources);
                    // AL.GetError();
        [DllImport(Lib, EntryPoint = "alGetError", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern ALError GetError();
                    // AL.GetSource(_source, ALGetSourcei.SourceState);
        [DllImport(Lib, EntryPoint = "alGetSourcei", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void GetSource(int sid, ALGetSourcei param, [Out] out int value);
        [DllImport(Lib, EntryPoint = "alGetSource3i", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void GetSource(int sid, ALSource3i param, out int value1, out int value2, out int value3);
        [DllImport(Lib, EntryPoint = "alGetSourcef", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void GetSource(int sid, ALSourcef param, out float value);
        [DllImport(Lib, EntryPoint = "alGetSource3f", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void GetSource(int sid, ALSource3f param, out float value1, out float value2, out float value3);
                    // AL.Source(source, ALSourcef.Pitch, 1);
        [DllImport(Lib, EntryPoint = "alSourcef", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void Source(int sid, ALSourcef param, float value);
        [DllImport(Lib, EntryPoint = "alSource3f", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void Source(int sid, ALSource3f param, float value1, float value2, float value3);
        [DllImport(Lib, EntryPoint = "alSourcei", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void Source(int sid, ALSourcei param, int value);
        [DllImport(Lib, EntryPoint = "alSource3i", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void Source(int sid, ALSource3i param, int value1, int value2, int value3);
                    // AL.SourcePause(_source);
        [DllImport(Lib, EntryPoint = "alSourcePausev", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void SourcePause(int ns, [In] int* sids);
        [DllImport(Lib, EntryPoint = "alSourcePause", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void SourcePause(int sid);
                    // AL.SourcePlay(_source);
        [DllImport(Lib, EntryPoint = "alSourcePlay", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static extern void SourcePlay(int sid);
        [DllImport(Lib, EntryPoint = "alSourcePlayv", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void SourcePlay(int ns, [In] int* sids);
                    // AL.SourceQueueBuffer(_source, buffer); //Queue buffer back into player.
                    // AL.SourceQueueBuffers(_source, _buffers); //Queue buffers.
        [DllImport(Lib, EntryPoint = "alSourceQueueBuffers", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void SourceQueueBuffers(int sid, int numEntries, [In] int* bids);
                    // AL.SourceUnqueueBuffers(_source, processedBuffers);
        [DllImport(Lib, EntryPoint = "alSourceUnqueueBuffers", ExactSpelling = true, CallingConvention = ALCallingConvention)]
        public static unsafe extern void SourceUnqueueBuffers(int sid, int numEntries, int* bids);

                    // ALC.CloseDevice(device); //Close/Dispose created/opened device.
        [DllImport(Lib, EntryPoint = "alcCloseDevice", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern bool CloseDevice([In] ALDevice device);
                    // ALC.CreateContext(device, (int[]?)null);
        [DllImport(Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static unsafe extern ALContext CreateContext([In] ALDevice device, [In] int* attributeList);
                    // ALC.DestroyContext(_deviceContext);
        [DllImport(Lib, EntryPoint = "alcDestroyContext", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern void DestroyContext(ALContext context);
                    // ALC.MakeContextCurrent(_deviceContext);
        [DllImport(Lib, EntryPoint = "alcMakeContextCurrent", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern bool MakeContextCurrent(ALContext context);
                    // ALC.OpenDevice(deviceName);
        [DllImport(Lib, EntryPoint = "alcOpenDevice", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)]
        public static extern ALDevice notOpenDevice([In] string devicename);
                    // ALC.ProcessContext(context);
        [DllImport(Lib, EntryPoint = "alcProcessContext", ExactSpelling = true, CallingConvention = AlcCallingConv)]
        public static extern void ProcessContext(ALContext context);



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
        private int _bufferBytes;
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
                    throw new InvalidOperationException(Locales.Locales.Audio_Player_InitFailed);

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
                
                var blockAlign = Channels * (BitDepth / 8);
                var bytesPerSecond = _sampleRate * blockAlign;
                var bufferMs = (BufferMilliseconds + NumberOfBuffers - 1) / NumberOfBuffers;
                _bufferBytes = (int) (bytesPerSecond / 1000.0 * bufferMs);
                if (_bufferBytes % blockAlign != 0)
                {
                    _bufferBytes = _bufferBytes + blockAlign - _bufferBytes % blockAlign;
                }
                
                //Open and setup device.
                _nativePlayer = ALC.OpenDevice(SelectedDevice);
                if (_nativePlayer == ALDevice.Null)
                    throw new InvalidOperationException(Locales.Locales.Audio_Player_InitFailed);

                _nativePlayerContext = ALC.CreateContext(_nativePlayer, (int[]?)null);
                if (_nativePlayerContext == ALContext.Null)
                    throw new InvalidOperationException(Locales.Locales.Audio_Player_InitFailed);
                
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
                    buffer.Dispose();
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
                throw new InvalidOperationException(Locales.Locales.Audio_Player_Init);
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
                throw new InvalidOperationException();

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
                if (processedBuffers <= 0)
                {
                    Thread.Sleep(1); //So we don't exactly burn the CPU. Small hack but it works.
                    continue;
                }

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

        private class AudioBuffer(int id, int size, int sampleRate, ALFormat format) : IDisposable
        {
            public int Id { get; } = id;
            public byte[] Data { get; } = GC.AllocateArray<byte>(size, true);
            private bool _isDisposed;

            ~AudioBuffer()
            {
                Dispose(false);
            }

            public void Clear()
            {
                Array.Clear(Data, 0, Data.Length);
            }

            public unsafe void FillBuffer(int read)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, typeof(AudioBuffer).ToString());
                
                fixed (byte* byteBufferPtr = Data)
                    AL.BufferData(Id, format, byteBufferPtr, read, sampleRate);
            }

            public void SourceQueue(int sourceId)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, typeof(AudioBuffer).ToString());
                
                AL.SourceQueueBuffer(sourceId, Id);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            
            private void Dispose(bool _)
            {
                if (_isDisposed) return;

                AL.DeleteBuffer(Id);

                _isDisposed = true;
            }
        }
    }
}
