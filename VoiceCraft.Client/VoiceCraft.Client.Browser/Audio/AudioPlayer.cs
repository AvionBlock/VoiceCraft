using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private IWaveProvider? _waveProvider;
        private ALDevice _device;
        private ALContext _deviceContext;
        private ALFormat _format;
        private float _volume = 1.0f;
        private int _source;
        private int _bufferSize;
        private int[] _buffers = [];
        private bool _disposed;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                if (_source != 0)
                    AL.Source(_source, ALSourcef.Gain, _volume);
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
            //Dispose of this object.
            Dispose(false);
        }

        public void Init(IWaveProvider waveProvider)
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if already playing.
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException("Can't re-initialize during playback");

            //Close previous audio device if it hasn't been closed by the previous initialization.
            if (_device != ALDevice.Null || _deviceContext != ALContext.Null)
                CloseDevice();

            //Create/Open new audio device.
            //Check if the format is supported first.
            _format = (waveProvider.WaveFormat.BitsPerSample, waveProvider.WaveFormat.Channels) switch
            {
                (8, 1) => ALFormat.Mono8,
                (8, 2) => ALFormat.Stereo8,
                (16, 1) => ALFormat.Mono16,
                (16, 2) => ALFormat.Stereo16,
                (32, 1) => ALFormat.MonoFloat32Ext,
                (32, 2) => ALFormat.StereoFloat32Ext,
                _ => throw new NotSupportedException("Input wave format is not supported!")
            };
            
            _waveProvider = waveProvider;
            _bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);
            (_device, _deviceContext) = OpenDevice(SelectedDevice);
            _buffers = GenerateBuffers(NumberOfBuffers);
            _source = GenerateSource();

            //Set output wave format.
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_device == ALDevice.Null || _deviceContext == ALContext.Null || _waveProvider == null)
                throw new InvalidOperationException("Must call Init first!");

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
            if (_device == ALDevice.Null || _deviceContext == ALContext.Null || _waveProvider == null)
                throw new InvalidOperationException("Must call Init first!");

            //Check if it has already been stopped.
            if (PlaybackState == PlaybackState.Stopped) return;

            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;

            //Block thread until it's fully stopped.
            while (_device != ALDevice.Null)
                Task.Delay(1).GetAwaiter().GetResult();
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if device is already closed/null.
            if (_device == ALDevice.Null || _deviceContext == ALContext.Null || _waveProvider == null)
                throw new InvalidOperationException("Must call Init first!");

            //Check if it has already been paused or is not playing.
            if (PlaybackState != PlaybackState.Playing) return;

            //Pause the wave player.
            AL.GetError();
            AL.SourcePause(_source);
            var error = AL.GetError();
            if(error != ALError.NoError)
                throw new InvalidOperationException($"Failed to pause playback! {error}");
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
            AL.GetError();
            AL.SourcePlay(_source);
            var error = AL.GetError();
            if(error != ALError.NoError)
                throw new InvalidOperationException($"Failed to resume playback! {error}");
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
                //Close previous audio device if it hasn't been closed by the previous initialization.
                if (_device != ALDevice.Null || _deviceContext != ALContext.Null)
                    CloseDevice();

                PlaybackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }
        
        private unsafe void PlaybackLogic()
        {
            if (_device == ALDevice.Null || _deviceContext == ALContext.Null || _waveProvider == null)
                throw new InvalidOperationException("Must call Init first!");
            
            //If state is newly created or has been stopped. Fill buffers and queue them.
            ALSourceState State() => (ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState);
            if (State() is ALSourceState.Initial or ALSourceState.Stopped)
            {
                foreach (var buffer in _buffers)
                {
                    var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(byteBuffer, 0, _bufferSize);
                    if (read > 0)
                    {
                        fixed (byte* byteBufferPtr = byteBuffer)
                            AL.BufferData(buffer, _format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
                    }
                }
                AL.SourceQueueBuffers(_source, _buffers); //Queue buffers.
            }
            
            AL.SourcePlay(_source);
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped && _device != ALDevice.Null && _deviceContext != ALContext.Null && _waveProvider != null)
            {
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                if (State() == ALSourceState.Stopped)
                    break; //Reached the end of the playback buffer or the audio player has stopped.
                //Get all buffers that have been processed.
                var processedBuffers = AL.GetSource(_source, ALGetSourcei.BuffersProcessed);
                if(processedBuffers <= 0) continue;
                
                //Unqueue the processed buffers.
                var buffers = AL.SourceUnqueueBuffers(_source, processedBuffers);
                foreach (var buffer in buffers)
                {
                    //Fill buffers with more data
                    var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(byteBuffer, 0, _bufferSize);
                    if (read <= 0) continue;
                    
                    fixed (byte* byteBufferPtr = byteBuffer)
                        AL.BufferData(buffer, _format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
                    
                    AL.SourceQueueBuffer(_source, buffer); //Queue buffer back into player.
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;
            if (PlaybackState != PlaybackState.Stopped)
            {
                Stop();
            }

            //Close previous audio device if it hasn't been closed by the previous initialization.
            if (_device != ALDevice.Null || _deviceContext != ALContext.Null)
                CloseDevice();

            _disposed = true;
        }

        private static (ALDevice, ALContext) OpenDevice(string? deviceName = null)
        {
            AL.GetError(); //Clear possible previous error
            var device = ALC.OpenDevice(deviceName);
            if (device == ALDevice.Null)
            {
                var error = AL.GetError();
                throw new InvalidOperationException($"Failed to open device {deviceName ?? "(default device)"}! {error}");
            }

            var context = ALC.CreateContext(device, (int[]?)null);
            if (context == ALContext.Null)
            {
                var error = AL.GetError();
                ALC.CloseDevice(device); //Close/Dispose created/opened device.
                throw new InvalidOperationException($"Could not create device context! {error}");
            }

            ALC.MakeContextCurrent(context);
            ALC.ProcessContext(context);
            return (device, context);
        }

        private static int[] GenerateBuffers(int numberOfBuffers)
        {
            AL.GetError(); //Clear possible previous error
            var buffers = AL.GenBuffers(numberOfBuffers);

            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
                throw new InvalidOperationException($"Failed to generate buffers! {error}");
            return buffers;
        }

        private int GenerateSource()
        {
            AL.GetError(); //Clear possible previous error
            var source = AL.GenSource();
            AL.Source(source, ALSourcef.Pitch, 1);
            AL.Source(source, ALSourcef.Gain, _volume);
            AL.Source(source, ALSourceb.Looping, false);
            AL.Source(source, ALSource3f.Position, 0, 0, 0);
            AL.Source(source, ALSource3f.Velocity, 0, 0, 0);
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
                throw new InvalidOperationException($"Failed to generate source! {error}");
            return source;
        }

        private void CloseDevice()
        {
            if (_source != 0)
            {
                AL.DeleteSource(_source);
                _source = 0;
            }

            if (_buffers.Length != 0)
            {
                AL.DeleteBuffers(_buffers);
                _buffers = [];
            }

            if (_deviceContext != ALContext.Null)
            {
                ALC.MakeContextCurrent(_deviceContext);
                ALC.DestroyContext(_deviceContext);
                _deviceContext = ALContext.Null;
            }

            if (_device == ALDevice.Null) return;
            ALC.CloseDevice(_device);
            _device = ALDevice.Null;
        }
    }
}
