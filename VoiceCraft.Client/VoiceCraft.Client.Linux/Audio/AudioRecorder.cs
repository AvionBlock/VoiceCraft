using System;
using System.Threading;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core;
using OpenTK.Audio.OpenAL;

namespace VoiceCraft.Client.Linux.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        //Public Properties
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Sample rate must be greater than or equal to zero!");

                _sampleRate = value;
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                if(value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }

        public int BitDepth
        {
            get
            {
                return (Format) switch
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
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice { get; set; }

        public CaptureState CaptureState { get; private set; }
        
        public event Action<byte[], int>? OnDataAvailable;
        public event Action<Exception?>? OnRecordingStopped;
        
        //Privates
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private ALCaptureDevice _nativeRecorder = ALCaptureDevice.Null;
        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;
        private int _bufferSamples;
        private int _bufferBytes;
        private int _blockAlign;
        private byte[] _buffer = [];
        private bool _disposed;
        
        public AudioRecorder(int sampleRate, int channels, AudioFormat format)
        {
            SampleRate = sampleRate;
            Channels = channels;
            Format = format;
        }

        ~AudioRecorder()
        {
            Dispose(false);
        }
        
        public void Initialize()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            
            if(CaptureState != CaptureState.Stopped)
                throw new InvalidOperationException("Cannot initialize when recording!");
            
            //Cleanup previous recorder.
            CleanupRecorder();

            try
            {
                //Select Device.
                var format = (Format, Channels) switch
                {
                    (AudioFormat.Pcm8, 1) => ALFormat.Mono8,
                    (AudioFormat.Pcm8, 2) => ALFormat.Stereo8,
                    (AudioFormat.Pcm16, 1) => ALFormat.Mono16,
                    (AudioFormat.Pcm16, 2) => ALFormat.Stereo16,
                    (AudioFormat.PcmFloat, 1) => AL.IsExtensionPresent("AL_EXT_float32")
                        ? ALFormat.MonoFloat32Ext
                        : throw new NotSupportedException(),
                    (AudioFormat.PcmFloat, 2) => AL.IsExtensionPresent("AL_EXT_float32")
                        ? ALFormat.StereoFloat32Ext
                        : throw new NotSupportedException(),
                    _ => throw new NotSupportedException()
                };

                //Setup recorder.
                _bufferSamples = BufferMilliseconds * SampleRate / 1000; //Calculate buffer size IN SAMPLES!
                _bufferBytes = BitDepth / 8 * Channels * _bufferSamples;
                _blockAlign = Channels * (BitDepth / 8);
                if (_bufferBytes % _blockAlign != 0)
                {
                    _bufferBytes -= _bufferBytes % _blockAlign;
                }
                _buffer = new byte[_bufferBytes];
                
                _nativeRecorder = ALC.CaptureOpenDevice(SelectedDevice, SampleRate, format, _bufferSamples);
                if (_nativeRecorder == ALCaptureDevice.Null)
                {
                    throw new InvalidOperationException("Could not create device!");
                }
            }
            catch
            {
                CleanupRecorder();
                throw;
            }
        }

        public void Start()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (CaptureState != CaptureState.Stopped) return;

            //Open Capture Device
            try
            {
                CaptureState = CaptureState.Starting;
                ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
                
                while (CaptureState == CaptureState.Starting)
                {
                    Thread.Sleep(1); //Wait until started.
                }
            }
            catch
            {
                CaptureState = CaptureState.Stopped;
                throw;
            }
        }

        public void Stop()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (CaptureState != CaptureState.Capturing) return;

            CaptureState = CaptureState.Stopping;
            ALC.CaptureStop(_nativeRecorder);
            
            while (CaptureState == CaptureState.Stopping)
            {
                Thread.Sleep(1); //Wait until stopped.
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void CleanupRecorder()
        {
            if (_nativeRecorder == ALCaptureDevice.Null) return;
            ALC.CaptureStop(_nativeRecorder);
            ALC.CaptureCloseDevice(_nativeRecorder);
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_nativeRecorder == ALCaptureDevice.Null)
                throw new InvalidOperationException("Audio recorder is not initialized!");
        }
        
        private void InvokeDataAvailable(byte[] buffer, int bytesRecorded)
        {
            CaptureState = CaptureState.Capturing;
            OnDataAvailable?.Invoke(buffer, bytesRecorded);
        }

        private void InvokeRecordingStopped(Exception? exception = null)
        {
            CaptureState = CaptureState.Stopped;
            var handler = OnRecordingStopped;
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

        private void RecordThread()
        {
            Exception? exception = null;
            try
            {
                RecordingLogic();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                ALC.CaptureStop(_nativeRecorder);
                CaptureState = CaptureState.Stopped;
                InvokeRecordingStopped(exception);
            }
        }

        private unsafe void RecordingLogic()
        {
            ALC.CaptureStart(_nativeRecorder);
            CaptureState = CaptureState.Capturing;
            var capturedSamples = 0;

            //Run the record loop
            while (CaptureState == CaptureState.Capturing && _nativeRecorder != ALCaptureDevice.Null)
            {
                // Query the number of captured samples
                ALC.GetInteger(_nativeRecorder, AlcGetInteger.CaptureSamples, sizeof(int), &capturedSamples);

                if (capturedSamples < _bufferSamples) continue;
                Array.Clear(_buffer);
                fixed (void* bufferPtr = _buffer)
                    ALC.CaptureSamples(_nativeRecorder, bufferPtr, _bufferSamples);
                
                InvokeDataAvailable(_buffer, _bufferBytes);
            }
        }

        private void Dispose(bool _)
        {
            if(_disposed) return;
            
            //Recorder isn't managed.
            CleanupRecorder();
            
            _disposed = true;
        }
    }
}