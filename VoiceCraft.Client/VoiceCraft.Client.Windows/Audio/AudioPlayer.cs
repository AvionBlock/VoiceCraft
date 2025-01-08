using System;
using System.Threading;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
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
                Thread.Sleep(1);
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