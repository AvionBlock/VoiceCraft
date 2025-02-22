using System;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClient : IDisposable
    {
        public const int FrameSizeMs = 20;
        public static readonly Version Version = new Version(1, 1, 0);
        public static readonly WaveFormat WaveFormat = new WaveFormat(48000, 1);
        public static readonly uint SamplesPerFrame = (uint)(WaveFormat.SampleRate / (1000 / FrameSizeMs) * WaveFormat.Channels);
        public static readonly uint BytesPerFrame = (uint)WaveFormat.ConvertLatencyToByteSize(FrameSizeMs);

        public int Ping { get; private set; }
        public ConnectionStatus ConnectionStatus { get; private set; }
        public Guid PublicId { get; private set; }

        //Network Events
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        public event Action<int>? OnLatencyUpdated;

        //Packet Events
        public event Action<ServerInfoPacket>? OnServerInfoPacketReceived;

        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter;
        private readonly OpusEncoder _encoder;
        private readonly BufferedWaveProvider _queuedAudio;
        private readonly byte[] _extractedAudioBuffer;
        private readonly byte[] _encodedAudioBuffer;
        private NetPeer? _serverPeer;
        private bool _isDisposed;
        private uint _currentTimestamp;

        public VoiceCraftClient()
        {
            _dataWriter = new NetDataWriter();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            _encoder = new OpusEncoder(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            _queuedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = false, BufferDuration = TimeSpan.FromSeconds(2) }; //2 seconds.
            _extractedAudioBuffer = new byte[BytesPerFrame];
            _encodedAudioBuffer = new byte[1000];

            _listener.PeerConnectedEvent += OnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        public void Connect(string ip, int port, LoginType loginType)
        {
            ThrowIfDisposed();
            if (ConnectionStatus != ConnectionStatus.Disconnected)
                throw new InvalidOperationException("You must disconnect before connecting!");

            if (!_netManager.IsRunning)
                _netManager.Start();

            ConnectionStatus = ConnectionStatus.Connecting;
            var loginPacket = new LoginPacket()
            {
                Version = Version.ToString(),
                LoginType = loginType,
                PositioningType = PositioningType.Server
            };
            _dataWriter.Reset();
            loginPacket.Serialize(_dataWriter);
            _netManager.Connect(ip, port, _dataWriter);
        }

        public void Update()
        {
            _netManager.PollEvents();
            if (_queuedAudio.BufferedBytes < BytesPerFrame) return;
            Array.Clear(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            Array.Clear(_encodedAudioBuffer, 0, _encodedAudioBuffer.Length);
            _queuedAudio.Read(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            _currentTimestamp += SamplesPerFrame;
            //Turns out RTPIncrement is samples per frame. IDK how this shit works...
            var encodedBytes = _encoder.Encode(_extractedAudioBuffer, (int)SamplesPerFrame, _encodedAudioBuffer, _encodedAudioBuffer.Length);
            //We don't need to input our public ID.
            SendPacket(new EntityAudioPacket() { Timestamp = _currentTimestamp, Data = _encodedAudioBuffer, DataLength = encodedBytes },
                DeliveryMethod.Unreliable);
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            if (ConnectionStatus == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Must be connecting or connected before disconnecting!");

            if (_serverPeer != null)
                _netManager.DisconnectPeer(_serverPeer);

            Update();
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (_serverPeer?.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            _serverPeer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        public void WriteAudio(byte[] buffer, int length)
        {
            _queuedAudio.AddSamples(buffer, 0, length);
        }

        //Events
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            ConnectionStatus = ConnectionStatus.Connected;
            _serverPeer = peer;
            OnConnected?.Invoke();
        }

        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ConnectionStatus = ConnectionStatus.Disconnected;
            _serverPeer = null;
            OnDisconnected?.Invoke(disconnectInfo);
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.ServerInfo:
                    var serverInfoPacket = new ServerInfoPacket();
                    serverInfoPacket.Deserialize(reader);
                    OnServerInfoPacketReceive(serverInfoPacket, peer);
                    break;
                case PacketType.Login:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            reader.Recycle();
        }

        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            Ping = latency;
            OnLatencyUpdated?.Invoke(latency);
        }

        private static void OnConnectionRequestEvent(ConnectionRequest request)
        {
            request.Reject();
        }

        //Packet Events
        private void OnServerInfoPacketReceive(ServerInfoPacket packet, NetPeer peer)
        {
            OnServerInfoPacketReceived?.Invoke(packet);
        }

        private void ThrowIfDisposed()
        {
            if (!_isDisposed) return;
            throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
        }

        //Dispose
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
                _queuedAudio.ClearBuffer();
            }

            _isDisposed = true;
        }
    }
}