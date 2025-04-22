using System;
using System.Linq;
using System.Net;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using VoiceCraft.Client.Network.Systems;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClient : VoiceCraftEntity, IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);

        //Network Events
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;

        //Public Properties
        public ConnectionState ConnectionState => _serverPeer?.ConnectionState ?? ConnectionState.Disconnected;
        public bool Muted { get; set; }
        public bool Deafened { get; set; }
        public float MicrophoneSensitivity { get; set; }

        //Systems
        public EventBasedNetListener Listener { get; } = new();
        public VoiceCraftWorld World { get; } = new();
        public NetworkSystem NetworkSystem { get; }

        //Privates
        private NetPeer? _serverPeer;
        private readonly NetManager _netManager;
        private readonly OpusEncoder _encoder;

        //Buffers
        private readonly BufferedWaveProvider _outputBuffer = new(new WaveFormat(Constants.SampleRate, Constants.Channels))
        {
            ReadFully = true,
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(1)
        };

        private readonly NetDataWriter _dataWriter = new();
        private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];
        private DateTime _lastAudioPeakTime = DateTime.MinValue;
        private uint _sendTimestamp;

        private string? _disconnectReason;
        private bool _isDisposed;

        public VoiceCraftClient() : base(0)
        {
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false,
                UnconnectedMessagesEnabled = true
            };
            NetworkSystem = new NetworkSystem(this);
            _encoder = new OpusEncoder(Constants.SampleRate, Constants.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            _encoder.SetPacketLostPercent(50); //Expected packet loss, might make this change over time later.
            _encoder.SetBitRate(32000);
            _netManager.Start();

            Listener.PeerConnectedEvent += InvokeConnected;
            Listener.PeerDisconnectedEvent += InvokeDisconnected;
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
                SendUnconnectedPacket(ip, port, packet);
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
            _serverPeer = _netManager.Connect(ip, port, dataWriter) ?? throw new InvalidOperationException("A connection request is awaiting!");
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (ConnectionState != ConnectionState.Connected) return false;

            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                _serverPeer?.Send(_dataWriter, deliveryMethod);
                return true;
            }
        }

        public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
            }
        }

        public bool SendUnconnectedPacket<T>(string ip, uint port, T packet) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, ip, (int)port);
            }
        }

        public void Update()
        {
            _netManager.PollEvents();
            if (ConnectionState == ConnectionState.Disconnected) return;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var clientEntities = World.Entities.OfType<VoiceCraftClientEntity>();
            foreach (var clientEntity in clientEntities)
            {
                var read = clientEntity.Read(buffer, 0, buffer.Length);
                if(read > 0)
                    _outputBuffer.AddSamples(buffer, 0, read);
            }
            
            return _outputBuffer.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int bytesRead)
        {
            var frameLoudness = GetFrameLoudness(buffer, bytesRead);
            if (frameLoudness >= MicrophoneSensitivity)
                _lastAudioPeakTime = DateTime.UtcNow;

            _sendTimestamp += Constants.SamplesPerFrame; //Add to timestamp even though we aren't really connected.
            if ((DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs || _serverPeer == null ||
                ConnectionState != ConnectionState.Connected || Muted) return;
            Array.Clear(_encodeBuffer);
            var bytesEncoded = _encoder.Encode(buffer, Constants.SamplesPerFrame, _encodeBuffer, _encodeBuffer.Length);
            var packet = new AudioPacket((byte)_serverPeer.RemoteId, _sendTimestamp, bytesEncoded, _encodeBuffer);
            SendPacket(packet);
        }

        public void Disconnect(string? reason = null)
        {
            if (_isDisposed || ConnectionState == ConnectionState.Disconnected) return;
            _disconnectReason = reason;
            _netManager.DisconnectAll();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (!_isDisposed) return;
            throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
        }

        private void InvokeConnected(NetPeer peer)
        {
            if (!Equals(peer, _serverPeer)) return;
            OnConnected?.Invoke();
        }

        private void InvokeDisconnected(NetPeer peer, DisconnectInfo info)
        {
            if (!Equals(peer, _serverPeer)) return;
            try
            {
                _outputBuffer.ClearBuffer();
                World.Clear();

                if (string.IsNullOrWhiteSpace(_disconnectReason))
                {
                    var reason = !info.AdditionalData.IsNull
                        ? Encoding.UTF8.GetString(info.AdditionalData.GetRemainingBytesSpan())
                        : info.Reason.ToString();
                    OnDisconnected?.Invoke(reason);
                    return;
                }

                OnDisconnected?.Invoke(_disconnectReason);
            }
            catch
            {
                OnDisconnected?.Invoke(info.Reason.ToString());
            }
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
            }

            _isDisposed = true;
        }

        private static float GetFrameLoudness(byte[] data, int bytesRead)
        {
            float max = 0;
            // interpret as 16-bit audio
            for (var index = 0; index < bytesRead; index += 2)
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