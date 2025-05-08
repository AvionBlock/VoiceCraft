using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClientEntity : VoiceCraftEntity
    {
        public event Action<bool, VoiceCraftEntity>? OnIsVisibleUpdated;
        public event Action<float, VoiceCraftEntity>? OnVolumeUpdated;
        
        public Guid? UserGuid { get; private set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnIsVisibleUpdated?.Invoke(_isVisible, this);
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                if(Math.Abs(_volume - value) < Constants.FloatingPointTolerance) return;
                _volume = value;
                OnVolumeUpdated?.Invoke(_volume, this);
            }
        }

        private readonly EntityType _entityType;
        private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);
        private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
        private readonly byte[] _encodedData = new byte[Constants.MaximumEncodedBytes];
        private readonly byte[] _readBuffer = new byte[Constants.BytesPerFrame];
        private DateTime _lastPacket = DateTime.MinValue;
        private bool _isReady;
        private bool _isVisible;
        private float _volume = 1f;

        private readonly BufferedWaveProvider _outputBuffer = new(new WaveFormat(Constants.SamplesPerFrame, Constants.Channels))
        {
            ReadFully = false,
            DiscardOnBufferOverflow = true
        };

        public VoiceCraftClientEntity(int id, EntityType entityType, VoiceCraftWorld world) : base(id, world)
        {
            _entityType = entityType;
            StartJitterThread();
        }

        public override void Deserialize(NetDataReader reader)
        {
            if (_entityType == EntityType.Network)
                UserGuid = reader.GetGuid();
            
            base.Deserialize(reader);
        }

        public void ClearBuffer()
        {
            _outputBuffer.ClearBuffer();
            lock (_jitterBuffer)
            {
                _jitterBuffer.Reset(); //Also reset the jitter buffer.
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!_isReady) return 0;
            var read = _outputBuffer.Read(buffer, offset, count);
            if (read == 0)
                _isReady = false;
            return read;
        }

        public override void ReceiveAudio(byte[] buffer, uint timestamp, float frameLoudness)
        {
            lock (_jitterBuffer)
            {
                var inPacket = new SpeexDSPJitterBufferPacket(buffer, (uint)buffer.Length)
                {
                    timestamp = timestamp,
                    span = Constants.SamplesPerFrame
                };
                _jitterBuffer.Put(ref inPacket);
            }

            base.ReceiveAudio(buffer, timestamp, frameLoudness);
        }

        public override void Destroy()
        {
            lock (_jitterBuffer)
            {
                _jitterBuffer.Dispose();
            }

            _decoder.Dispose();
            base.Destroy();
            
            OnIsVisibleUpdated = null;
            OnVolumeUpdated = null;
        }

        private int Read(byte[] buffer)
        {
            if (buffer.Length < Constants.BytesPerFrame)
                return 0;

            try
            {
                Array.Clear(_encodedData);
                var outPacket = new SpeexDSPJitterBufferPacket(_encodedData, (uint)_encodedData.Length);
                var startOffset = 0;
                lock (_jitterBuffer)
                {
                    if (_jitterBuffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                        return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                            ? 0
                            : _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
                }

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(_encodedData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
            }
            catch
            {
                return 0;
            }
            finally
            {
                lock (_jitterBuffer)
                {
                    _jitterBuffer.Tick();
                }
            }
        }

        private void StartJitterThread()
        {
            Task.Run(async () =>
            {
                var startTick = Environment.TickCount64;
                while (!Destroyed)
                {
                    try
                    {
                        var tick = Environment.TickCount;
                        var dist = startTick - tick;
                        if (dist > 0)
                        {
                            await Task.Delay((int)dist).ConfigureAwait(false);
                            continue;
                        }

                        startTick += Constants.FrameSizeMs;
                        Array.Clear(_readBuffer);
                        var read = Read(_readBuffer);
                        if (read > 0)
                            _outputBuffer.AddSamples(_readBuffer, 0, _readBuffer.Length);
                        
                        if(_outputBuffer.BufferedDuration >= TimeSpan.FromMilliseconds(Constants.DecodeBufferSizeThresholdMs))
                            _isReady = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            });
        }
    }
}