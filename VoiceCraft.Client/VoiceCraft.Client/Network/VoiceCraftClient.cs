using System;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using VoiceCraft.Client.Network.Systems;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClient : IDisposable, ISampleProvider
    {
        public static readonly Version Version = new(1, 1, 0);
        public static readonly WaveFormat AudioWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(Constants.SampleRate, Constants.Channels);
        public static readonly WaveFormat RecordWaveFormat = new(Constants.SampleRate, Constants.Channels);

        //Network Events
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;

        //Public Properties
        public WaveFormat WaveFormat => AudioWaveFormat;
        public ConnectionState ConnectionState => LocalEntity?.NetPeer.ConnectionState ?? ConnectionState.Disconnected;
        public VoiceCraftClientNetworkEntity? LocalEntity { get; private set; }
        public bool IsSpeaking => (DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds < Constants.SilenceThresholdMs;
        public bool Muted { get; set; }
        public bool Deafened { get; set; }

        public EventBasedNetListener Listener { get; } = new();
        public VoiceCraftWorld World { get; } = new();
        public NetworkSystem NetworkSystem { get; }

        private readonly NetManager _netManager;
        private readonly EntityAudioSystem _audioSystem;
        private readonly OpusEncoder _encoder = new(RecordWaveFormat.SampleRate, RecordWaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        
        //Buffers
        private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];
        private DateTime _lastAudioPeakTime = DateTime.MinValue;
        
        private uint _timestamp;
        private bool _isDisposed;

        public VoiceCraftClient()
        {
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false,
                UnconnectedMessagesEnabled = true
            };
            NetworkSystem = new NetworkSystem(this, _netManager);
            _audioSystem = new EntityAudioSystem(AudioWaveFormat, RecordWaveFormat, this);
            _netManager.Start();

            Listener.PeerConnectedEvent += peer =>
            {
                if (!Equals(peer, LocalEntity?.NetPeer)) return;
                OnConnected?.Invoke();
            };

            Listener.PeerDisconnectedEvent += (peer, info) =>
            {
                if (!Equals(peer, LocalEntity?.NetPeer)) return;
                try
                {
                    var reason = !info.AdditionalData.IsNull ? Encoding.UTF8.GetString(info.AdditionalData.GetRemainingBytesSpan()) : info.Reason.ToString();
                    OnDisconnected?.Invoke(reason);
                }
                catch
                {
                    OnDisconnected?.Invoke(info.Reason.ToString());
                }
            };
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        public bool Ping(string ip, uint port)
        {
            var packet = new InfoPacket(tick: Environment.TickCount);
            try
            {
                NetworkSystem.SendUnconnectedPacket(ip, port, packet);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Connect(string ip, int port, LoginType loginType)
        {
            ThrowIfDisposed();
            if (ConnectionState != ConnectionState.Disconnected)
                throw new InvalidOperationException("This client is already connected or is connecting to a server!");

            var dataWriter = new NetDataWriter();
            var loginPacket = new LoginPacket(Version.ToString(), loginType);
            loginPacket.Serialize(dataWriter);
            var serverPeer = _netManager.Connect(ip, port, dataWriter);
            LocalEntity = new VoiceCraftClientNetworkEntity(serverPeer) ?? throw new InvalidOperationException("A connection request is awaiting!");
        }

        public void Update()
        {
            _netManager.PollEvents();
            // if (ConnectionState == ConnectionState.Disconnected) return; //Not connected.
        }
        
        public int Read(float[] buffer, int offset, int count)
        {
            return _audioSystem.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if(count != Constants.BytesPerFrame || buffer.Length != Constants.BytesPerFrame)
                throw new InvalidOperationException($"Buffer does not match the expected number of {Constants.BytesPerFrame} bytes!");

            if (LocalEntity == null) return;
            Array.Clear(_encodeBuffer);
            _timestamp += Constants.SamplesPerFrame; //Increase timestamp. even if we don't send it.
            var frameLoudness = GetFrameLoudness(buffer);
            if (frameLoudness >= 0.03f)
                _lastAudioPeakTime = DateTime.UtcNow;

            if (!IsSpeaking) return;
            var encoded = _encoder.Encode(buffer, Constants.SamplesPerFrame, _encodeBuffer, _encodeBuffer.Length);
            var packet = new AudioPacket(LocalEntity.NetPeer.RemoteId, _timestamp, encoded, _encodeBuffer);
            NetworkSystem.SendPacket(packet);
        }

        public void Disconnect()
        {
            if (_isDisposed || ConnectionState == ConnectionState.Disconnected) return;
            _netManager.DisconnectAll();
        }

        #region Dispose

        private void ThrowIfDisposed()
        {
            if (!_isDisposed) return;
            throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                _encoder.Dispose();
                World.Dispose();
                NetworkSystem.Dispose();
                _audioSystem.Dispose();

                LocalEntity?.Destroy();
                LocalEntity = null;
            }

            _isDisposed = true;
        }

        #endregion

        private static float GetFrameLoudness(byte[] data)
        {
            float max = 0;
            // interpret as 16-bit audio
            for (var index = 0; index < data.Length; index += 2)
            {
                var sample = (short)((data[index + 1] << 8) |
                                       data[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            return max;
        }
    }
}