using Android.Media;
using NAudio.Wave;
using System;
using System.Threading;

namespace VoiceCraft.Client.Android.Audio
{
    public class AndroidAudioPlayer : IWavePlayer
    {
        private readonly SynchronizationContext? _synchronizationContext;
        private volatile PlaybackState _playbackState;
        private AutoResetEvent? _callbackEvent;
        private IWaveProvider? _waveStream;
        private AudioTrack? _audioTrack;
        private float _volume;

        public int DesiredLatency { get; set; }

        public WaveFormat? OutputWaveFormat => _waveStream?.WaveFormat;
        public PlaybackState PlaybackState => _playbackState;
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _audioTrack?.SetVolume(value);
            }
        }

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AndroidAudioPlayer()
        {
            _synchronizationContext = SynchronizationContext.Current;
            _playbackState = PlaybackState.Stopped;
            DesiredLatency = 300;
        }

        public void Init(IWaveProvider waveProvider)
        {
            if (_playbackState != 0)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }

            if (_audioTrack != null)
            {
                //Close audio track.
            }

            Encoding encoding;
            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm || waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                encoding = waveProvider.WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit, or 32-bit", nameof(waveProvider))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE float", nameof(waveProvider));
            }

            _callbackEvent = new AutoResetEvent(false);
            _waveStream = waveProvider;

            //Determine the channel mask
            ChannelOut channelMask = _waveStream.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(waveProvider))
            };

            //Determine the buffer size
            int minBufferSize = AudioTrack.GetMinBufferSize(_waveStream.WaveFormat.SampleRate, channelMask, encoding);
            int bufferSize = _waveStream.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)!
                    .SetContentType(AudioContentType.Music)!
                    .Build()!)
                .SetAudioFormat(new AudioFormat.Builder()!
                    .SetEncoding(encoding)!
                    .SetSampleRate(_waveStream.WaveFormat.SampleRate)!
                    .SetChannelMask(channelMask)!
                    .Build()!)
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .SetPerformanceMode(AudioTrackPerformanceMode.None)
                .Build();
            _audioTrack.SetVolume(Volume);
        }

        public void Play()
        {
            throw new NotImplementedException();
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
    }
}