using System;
using System.Threading;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Linux.Audio
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
            get => _volume;
            set => _volume = Math.Clamp(value, 0, 1);
        }

        public int DesiredLatency { get; set; } = 300;
        public string? Device { get; set; }
        public int NumberOfBuffers { get; set; } = 2;
        public WaveFormat OutputWaveFormat { get; private set; } = new();

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public void Init(IWaveProvider waveProvider)
        {
            ThrowIfDisposed();
            
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException("Can't re-initialize during playback");
            
            _audioDevice = new AudioDevice(waveProvider, DesiredLatency, NumberOfBuffers, Device);
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            ThrowIfDisposed();
            
            if (_audioDevice == null)
                throw new InvalidOperationException("Must call Init first");
            
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    PlaybackState = PlaybackState.Playing;
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
            ThrowIfDisposed();
            
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            if (PlaybackState == PlaybackState.Stopped) return;
            
            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;
            _audioDevice.Stop();
        }

        public void Pause()
        {
            ThrowIfDisposed();
            
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            if (PlaybackState == PlaybackState.Paused) return;
            
            //Stop the wave player
            PlaybackState = PlaybackState.Paused;
            _audioDevice.Pause();
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
            throw new ObjectDisposedException(typeof(AudioDevice).ToString());
        }
        
        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            PlaybackState = PlaybackState.Playing;
            _audioDevice?.Play();
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
                _audioDevice?.Dispose();
                _audioDevice = null;
                PlaybackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void PlaybackLogic()
        {
            if(_audioDevice == null) throw new InvalidOperationException("Must call Init first");
            
            _audioDevice.Play();
            while (PlaybackState != PlaybackState.Stopped && _audioDevice != null)
            {
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                var state = _audioDevice.GetState();
                if (state == ALSourceState.Stopped)
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
            _disposed = true;
        }

        private sealed class AudioDevice : IDisposable
        {
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
                    case (8, 1) :
                    case (8, 2) :
                    case (16, 1) :
                    case (16, 2) :
                    case (32, 1) :
                    case (32, 2) :
                        break;
                    default:
                        throw new NotSupportedException();
                }
                
                _waveProvider = waveProvider;
                _bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(desiredLatency);
                _device = OpenDevice(deviceName);
                _deviceContext = CreateContext();
                _buffers = AL.GenBuffers(numberOfBuffers);
                _source = CreateSource();
            }
            
            ~AudioDevice()
            {
                Dispose(false);
            }

            public void Play()
            {
                ThrowIfDisposed();
                
                if (GetState() is ALSourceState.Initial or ALSourceState.Stopped)
                {
                    FillBuffers();
                    AL.SourceQueueBuffers(_source, _buffers); //Queue buffers.
                }

                AL.SourcePlay(_source);
            }

            public void Pause()
            {
                ThrowIfDisposed();
                
                AL.SourcePause(_source);
            }

            public void Stop()
            {
                ThrowIfDisposed();
                
                AL.SourceStop(_source);
            }

            public ALSourceState GetState()
            {
                ThrowIfDisposed();
                
                return (ALSourceState)AL.GetSource(_source, ALGetSourcei.SourceState);
            }

            public unsafe void UpdateStream()
            {
                var processedBuffers = AL.GetSource(_source, ALGetSourcei.BuffersProcessed);
                if(processedBuffers <= 0) return;
                
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
                    var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(byteBuffer, 0, _bufferSize);
                    if (read > 0)
                    {
                        fixed (byte* byteBufferPtr = byteBuffer)
                            AL.BufferData(buffer, format, byteBufferPtr, read, _waveProvider.WaveFormat.SampleRate);
                    }
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
                AL.Source(source, ALSourceb.Looping, true);
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