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
        private AudioDevice? _audioDevice;
        private bool _disposed;
        private float _volume = 1.0f;

        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;

        public float Volume
        {
            get => _audioDevice?.Volume ?? _volume;
            set
            {
                if(_audioDevice != null)
                    _audioDevice.Volume = value;
                _volume = Math.Clamp(value, 0, 1);
            }
        }

        public int DesiredLatency { get; set; } = 300;
        public string? SelectedDevice { get; set; }
        public int NumberOfBuffers { get; set; } = 2;
        public WaveFormat OutputWaveFormat { get; private set; } = new();

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        ~AudioPlayer()
        {
            //Dispose of this object, kindof.
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
            if (_audioDevice != null)
            {
                _audioDevice.Dispose();
                _audioDevice = null;
            }
            
            //Create/Open new audio device.
            _audioDevice = new AudioDevice(waveProvider, DesiredLatency, NumberOfBuffers, SelectedDevice);
            _audioDevice.Volume = _volume; //Force set volume
            
            //Set output wave format.
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            //Check if device is already closed/null.
            if (_audioDevice == null)
                throw new InvalidOperationException("Must call Init first");
            
            //Resume or start playback.
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
                    //Block thread until it's fully started.
                    while(PlaybackState == PlaybackState.Stopped)
                        Thread.Sleep(1);
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
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            
            //Check if it has already been stopped.
            if (PlaybackState == PlaybackState.Stopped) return;
            
            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;
            
            //Block thread until it's fully stopped.
            while(_audioDevice != null)
                Thread.Sleep(1);
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            //Check if device is already closed/null.
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            
            //Check if it has already been paused or is not playing.
            if (PlaybackState != PlaybackState.Playing) return;
            
            //Pause the wave player.
            _audioDevice.Pause();
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
            _audioDevice?.Play();
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
                if (_audioDevice != null)
                {
                    _audioDevice.Stop();
                    _audioDevice.Dispose();
                    _audioDevice = null;
                }
                PlaybackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            
            _audioDevice.Play();
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped && _audioDevice != null)
            {
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                if (_audioDevice.State == ALSourceState.Stopped)
                    break; //Reached the end of the playback buffer or the audio player has stopped.
                _audioDevice.UpdateStream();
            }
        }
        
        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;
            if (PlaybackState != PlaybackState.Stopped)
            {
                Stop();
            }
            
            //Close audio device if it hasn't been closed by the player thread.
            if (_audioDevice != null)
            {
                _audioDevice.Dispose();
                _audioDevice = null;
            }
            
            _disposed = true;
        }

        private sealed class AudioDevice : IDisposable
        {
            public float Volume
            {
                get => AL.GetSource(_source, ALSourcef.Gain);
                set => AL.Source(_source, ALSourcef.Gain, Math.Clamp(value, 0, 1));
            }

            public ALSourceState State
            {
                get
                {
                    ThrowIfDisposed();
                    return (ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState);       
                }
            }

            private readonly IWaveProvider _waveProvider;
            private readonly ALDevice _device;
            private readonly ALContext _deviceContext;
            private readonly int _source;
            private readonly int _bufferSize;
            private readonly int[] _buffers;
            private bool _disposed;

            public AudioDevice(IWaveProvider waveProvider, int desiredLatency, int numberOfBuffers, string? deviceName = null)
            {
                switch (waveProvider.WaveFormat.BitsPerSample, waveProvider.WaveFormat.Channels)
                {
                    case (8, 1):
                    case (8, 2):
                    case (16, 1):
                    case (16, 2):
                    case (32, 1):
                    case (32, 2):
                        break;
                    default:
                        throw new NotSupportedException();
                }
                
                try
                {
                    _waveProvider = waveProvider;
                    _bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((desiredLatency + numberOfBuffers - 1) / numberOfBuffers);
                    _device = OpenDevice(deviceName);
                    _deviceContext = CreateContext();
                    _buffers = AL.GenBuffers(numberOfBuffers);
                    _source = CreateSource();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
            
            ~AudioDevice()
            {
                Dispose(false);
            }

            public void Play()
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                
                //If state is newly created or has been stopped. Fill buffers and queue them.
                if (State is ALSourceState.Initial or ALSourceState.Stopped)
                {
                    FillBuffers();
                    AL.SourceQueueBuffers(_source, _buffers); //Queue buffers.
                }

                //Play or resume source.
                AL.SourcePlay(_source);
            }

            public void Pause()
            {
                //Disposed? DIE!
                ThrowIfDisposed();

                //If not playing, we can't pause the source.
                if (State != ALSourceState.Playing) return;
                
                //Pause Source.
                AL.SourcePause(_source);
            }

            public void Stop()
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                
                //If not playing or paused, we can't stop the source.
                if (State is not (ALSourceState.Playing or ALSourceState.Paused)) return;
                
                //Stop the source.
                AL.SourceStop(_source);
            }

            public unsafe void UpdateStream()
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                
                //Get all buffers that have been processed.
                var processedBuffers = AL.GetSource(_source, ALGetSourcei.BuffersProcessed);
                if(processedBuffers <= 0) return;
                
                //Unqueue the processed buffers.
                var buffers = AL.SourceUnqueueBuffers(_source, processedBuffers);
                foreach (var buffer in buffers)
                {
                    var format = (_waveProvider.WaveFormat.BitsPerSample, _waveProvider.WaveFormat.Channels) switch
                    {
                        (8, 1) => ALFormat.Mono8,
                        (8, 2) => ALFormat.Stereo8,
                        (16, 1) => ALFormat.Mono16,
                        (16, 2) => ALFormat.Stereo16,
                        (32, 1) => ALFormat.MonoFloat32Ext,
                        (32, 2) => ALFormat.StereoFloat32Ext,
                        _ => throw new NotSupportedException()
                    };
                    
                    //Fill buffers with more data
                    var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(byteBuffer, 0, _bufferSize);
                    if (read <= 0) continue;
                    
                    fixed (byte* byteBufferPtr = byteBuffer)
                        AL.BufferData(buffer, format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
                    
                    AL.SourceQueueBuffer(_source, buffer); //Queue buffer back into player.
                }
            }
            
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private unsafe void FillBuffers()
            {
                var format = (_waveProvider.WaveFormat.BitsPerSample, _waveProvider.WaveFormat.Channels) switch
                {
                    (8, 1) => ALFormat.Mono8,
                    (8, 2) => ALFormat.Stereo8,
                    (16, 1) => ALFormat.Mono16,
                    (16, 2) => ALFormat.Stereo16,
                    (32, 1) => ALFormat.MonoFloat32Ext,
                    (32, 2) => ALFormat.StereoFloat32Ext,
                    _ => throw new NotSupportedException()
                };
                    
                foreach (var buffer in _buffers)
                {
                    var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(byteBuffer, 0, _bufferSize);
                    if (read > 0)
                    {
                        fixed (byte* byteBufferPtr = byteBuffer)
                            AL.BufferData(buffer, format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
                    }
                }
            }
            
            private ALContext CreateContext()
            {
                var context = ALC.CreateContext(_device, (int[]?)null);
                if (context == ALContext.Null)
                {
                    throw new InvalidOperationException("Could not create device context!");
                }

                ALC.MakeContextCurrent(context);
                ALC.ProcessContext(context);

                return context;
            }
            
            private static int CreateSource()
            {
                var source = AL.GenSource();
                AL.Source(source, ALSourcef.Pitch, 1);
                AL.Source(source, ALSourcef.Gain, 1);
                AL.Source(source, ALSourceb.Looping, false);
                AL.Source(source, ALSource3f.Position, 0, 0, 0);
                AL.Source(source, ALSource3f.Velocity, 0, 0, 0);
                return source;
            }

            private static ALDevice OpenDevice(string? deviceName = null)
            {
                var device = ALC.OpenDevice(deviceName);
                if (device == ALDevice.Null)
                {
                    throw new InvalidOperationException("Could not create device!");
                }

                return device;
            }

            private void Dispose(bool disposing)
            {
                if (_disposed) return;
                if (disposing)
                {
                    AL.DeleteSource(_source);
                    AL.DeleteBuffers(_buffers);
                    ALC.MakeContextCurrent(_deviceContext);
                    ALC.DestroyContext(_deviceContext);
                    ALC.CloseDevice(_device);
                }

                _disposed = true;
            }
            
            private void ThrowIfDisposed()
            {
                if (!_disposed) return;
                throw new ObjectDisposedException(typeof(AudioDevice).ToString());
            }
        }
    }
}