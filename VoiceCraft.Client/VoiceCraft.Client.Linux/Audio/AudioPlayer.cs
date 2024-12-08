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
        private IWaveProvider? _waveProvider;
        private ALDevice _device = ALDevice.Null;
        private ALContext _deviceContext = ALContext.Null;
        private ALFormat _format;
        private int[]? _buffers;
        private int _bufferSize;
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
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException("Can't re-initialize during playback");
            
            _waveProvider = waveProvider;
            OutputWaveFormat = waveProvider.WaveFormat;
            _device = OpenDevice(Device);
            _deviceContext = CreateContext(_device);
            _buffers = CreateBuffers(NumberOfBuffers);
            _bufferSize = OutputWaveFormat.ConvertLatencyToByteSize(DesiredLatency);

            _format = (OutputWaveFormat.BitsPerSample, OutputWaveFormat.Channels) switch
            {
                (8, 1) => ALFormat.Mono8,
                (8, 2) => ALFormat.Stereo8,
                (16, 1) => ALFormat.Mono16,
                (16, 2) => ALFormat.Stereo16,
                _ => throw new NotSupportedException()
            };
        }

        public void Play()
        {
            if (_waveProvider == null || _buffers == null)
                throw new InvalidOperationException("Must call Init first");
            
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    PlaybackState = PlaybackState.Playing;
                    ThreadPool.QueueUserWorkItem(_ => PlaybackThread(_waveProvider, _buffers), null);
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
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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
        
        private unsafe void PlaybackThread(IWaveProvider waveProvider, int[] buffers)
        {
            Exception? exception = null;
            var source = -1;
            try
            {
                var byteBuffer = GC.AllocateArray<byte>(_bufferSize, true);
                fixed (void* byteBufferPtr = byteBuffer)
                    foreach (var buffer in buffers)
                    {
                        //Write audio data into the buffers.
                        var read = waveProvider.Read(byteBuffer, 0, _bufferSize);
                        if (read > 0)
                        {
                            AL.BufferData(buffer, _format, byteBufferPtr, read, OutputWaveFormat.SampleRate);
                        }
                    }
                source = CreateSource(_volume, buffers);
                
                PlaybackLogic(source);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                PlaybackState = PlaybackState.Stopped;
                CloseDevice(source, buffers, _deviceContext, _device);
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private unsafe void PlaybackLogic(int source)
        {
            if(_waveProvider == null)
                throw new InvalidOperationException("Could not resolve wave provider!");
            
            while (PlaybackState != PlaybackState.Stopped)
            {
                var processed = AL.GetSource(source, ALGetSourcei.BuffersProcessed);
                if(processed <= 0) continue;
                var bufferIds = AL.SourceUnqueueBuffers(source, processed);
                foreach (var bufferId in bufferIds)
                {
                    var data = GC.AllocateArray<byte>(_bufferSize, true);
                    var read = _waveProvider.Read(data, 0, _bufferSize);
                    fixed (void* dataPtr = data)
                    {
                        if (read > 0)
                            AL.BufferData(bufferId, _format, dataPtr, read, OutputWaveFormat.SampleRate);
                    }
                }
                AL.SourceQueueBuffers(source, bufferIds);
            }
        }

        private static int CreateSource(float volume, int[] buffersIds)
        {
            var source = AL.GenSource();
            AL.Source(source, ALSourcef.Pitch, 1);
            AL.Source(source, ALSourcef.Gain, volume);
            AL.Source(source, ALSourceb.Looping, true);
            AL.Source(source, ALSource3f.Position, 0, 0, 0);
            AL.Source(source, ALSource3f.Velocity, 0, 0, 0);
            AL.SourceQueueBuffers(source, buffersIds);
            return source;
        }

        private static int[] CreateBuffers(int numberOfBuffers)
        {
            return AL.GenBuffers(numberOfBuffers);
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

        private static ALContext CreateContext(ALDevice device)
        {
            var context = ALC.CreateContext(device, []);
            if (context == ALContext.Null)
            {
                throw new InvalidOperationException("Could not create device context!");
            }

            ALC.MakeContextCurrent(context);
            ALC.ProcessContext(context);

            return context;
        }

        private static void CloseDevice(int source, int[] buffers, ALContext context, ALDevice device)
        {
            if(source >= 0)
                AL.DeleteSource(source);
            
            AL.DeleteBuffers(buffers);
            if (context != ALContext.Null)
            {
                ALC.MakeContextCurrent(context);
                ALC.DestroyContext(context);
            }
            if (device == ALDevice.Null) return;
            
            ALC.CloseDevice(device);
        }
    }
}