using Android.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VoiceCraft.Client.PDK.Audio;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioRecorder : IAudioRecorder
    {
        //This may or may not include bugged devices that can crash the application.
        private static AudioDeviceType[] _allowedDeviceTypes = [
            AudioDeviceType.AuxLine,
            AudioDeviceType.BluetoothA2dp,
            AudioDeviceType.BluetoothSco,
            AudioDeviceType.BuiltinMic,
            AudioDeviceType.BuiltinEarpiece,
            AudioDeviceType.BuiltinSpeaker,
            AudioDeviceType.Dock,
            AudioDeviceType.Hdmi,
            AudioDeviceType.HdmiArc,
            AudioDeviceType.LineAnalog,
            AudioDeviceType.LineDigital,
            AudioDeviceType.UsbAccessory,
            AudioDeviceType.UsbDevice,
            AudioDeviceType.WiredHeadphones,
            AudioDeviceType.WiredHeadset
            ];

        private readonly SynchronizationContext? _synchronizationContext;
        private string? _selectedDevice;
        private AudioManager _audioManager;
        private CaptureState _captureState;
        private AudioRecord? _audioRecord;

        public WaveFormat WaveFormat { get; set; }
        public int BufferMilliseconds { get; set; }
        public AudioSource audioSource { get; set; }
        public bool IsRecording => _captureState == CaptureState.Capturing;

        public event EventHandler<WaveInEventArgs>? DataAvailable;
        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public AudioRecorder(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _synchronizationContext = SynchronizationContext.Current;

            audioSource = AudioSource.Mic;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            _captureState = CaptureState.Stopped;
        }

        public void StartRecording()
        {
            //Starting capture procedure
            OpenRecorder();

            //Check if we are already recording.
            if (_captureState == CaptureState.Capturing)
            {
                return;
            }

            //Make sure that we have some format to use.
            if (WaveFormat == null)
            {
                throw new ArgumentNullException(nameof(WaveFormat));
            }

            _captureState = CaptureState.Starting;
            var selectedDevice = _audioManager.GetDevices(GetDevicesTargets.Inputs)?.Where(x => _allowedDeviceTypes.Contains(x.Type)).FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == _selectedDevice); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.

            _audioRecord?.SetPreferredDevice(selectedDevice);
            _audioRecord?.StartRecording();
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        public void StopRecording()
        {
            if (_audioRecord == null)
            {
                return;
            }

            //Check if it has already been stopped
            if (_captureState != CaptureState.Stopped)
            {
                _captureState = CaptureState.Stopped;
                CloseRecorder();
            }
        }

        public void SetDevice(string device)
        {
            _selectedDevice = device;
        }

        public string GetDefaultDevice()
        {
            return "Default";
        }

        public List<string> GetDevices()
        {
            var devices = new List<string>() { GetDefaultDevice() };

            var audioDevices = _audioManager.GetDevices(GetDevicesTargets.Inputs)?.Where(x => _allowedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            if (audioDevices == null) return devices;

            foreach (var audioDevice in audioDevices)
            {
                var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }
            return devices;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_captureState != CaptureState.Stopped)
                {
                    StopRecording();
                }
                _audioRecord?.Release();
                _audioRecord?.Dispose();
                _audioRecord = null;
            }
        }

        private void OpenRecorder()
        {
            //We want to make sure the recorder is definitely closed.
            CloseRecorder();
            Encoding encoding;
            ChannelIn channelMask;

            //Set the encoding
            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm || WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                encoding = WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit or 32bit", nameof(WaveFormat))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE Float", nameof(WaveFormat));
            }

            //Set the channel type. Only accepts Mono or Stereo
            channelMask = WaveFormat.Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(WaveFormat))
            };

            //Determine the buffer size
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            //Determine min buffer size.
            var minBufferSize = AudioRecord.GetMinBufferSize(WaveFormat.SampleRate, channelMask, encoding);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }
            //Create the AudioRecord Object.
            _audioRecord = new AudioRecord(audioSource, WaveFormat.SampleRate, channelMask, encoding, bufferSize);
        }

        private void CloseRecorder()
        {
            //Make sure that the recorder was opened
            if (_audioRecord != null && _audioRecord.RecordingState != RecordState.Stopped)
            {
                _audioRecord.Stop();
                _audioRecord.Release();
                _audioRecord.Dispose();
                _audioRecord = null;
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
                _captureState = CaptureState.Stopped;
                CloseRecorder();
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void RaiseRecordingStoppedEvent(Exception? e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (_synchronizationContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    _synchronizationContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        private void RecordingLogic()
        {
            //Initialize the wave buffer
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            _captureState = CaptureState.Capturing;

            //Run the record loop
            while (_captureState != CaptureState.Stopped && _audioRecord != null)
            {
                if (_captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    byte[] byteBuffer = new byte[bufferSize];
                    var bytesRead = _audioRecord.Read(byteBuffer, 0, bufferSize);
                    if (bytesRead > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, bytesRead));
                    }
                    else if (bytesRead < 0 && _audioRecord.RecordingState != RecordState.Recording)
                    {
                        throw new Exception("An error occured while trying to capture data.");
                    }
                }
                else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    float[] floatBuffer = new float[bufferSize / 4];
                    byte[] byteBuffer = new byte[bufferSize];
                    var floatsRead = _audioRecord.Read(floatBuffer, 0, floatBuffer.Length, 0);
                    Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                    if (floatsRead > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(byteBuffer, floatsRead * 4));
                    }
                    else if (floatsRead < 0 && _audioRecord.RecordingState != RecordState.Recording)
                    {
                        throw new Exception("An error occured while trying to capture data.");
                    }
                }
            }
        }
    }
}