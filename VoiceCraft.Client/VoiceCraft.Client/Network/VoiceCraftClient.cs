using System;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using VoiceCraft.Client.Network.EventHandlers;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClient : IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);
        public static readonly WaveFormat WaveFormat = new(AudioConstants.SampleRate, AudioConstants.Channels);
        private static readonly uint BytesPerFrame = (uint)WaveFormat.ConvertLatencyToByteSize(AudioConstants.FrameSizeMs);

        public int Latency { get; internal set; }
        public ConnectionStatus ConnectionStatus { get; internal set; }

        //Network Events
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        
        
        //Public Properties
        public EventBasedNetListener Listener { get; } = new();
        public NetPeer? ServerPeer { get; internal set; }
        
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter = new();
        private readonly NetworkEventHandler _networkEventHandler;
        
        private readonly OpusEncoder _encoder;
        private readonly BufferedWaveProvider _queuedAudio;
        private readonly byte[] _extractedAudioBuffer = new byte[BytesPerFrame];
        private readonly byte[] _encodedAudioBuffer = new byte[1000];
        private bool _isDisposed;
        private uint _currentTimestamp;

        public VoiceCraftClient()
        {
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            _networkEventHandler = new NetworkEventHandler(this);
            
            _encoder = new OpusEncoder(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            _queuedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = false, BufferDuration = TimeSpan.FromSeconds(2) }; //2 seconds.
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }
        
        #region Public Methods
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
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            if (ConnectionStatus == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Must be connecting or connected before disconnecting!");
            
            _netManager.DisconnectAll();
            Update();
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (ServerPeer?.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            ServerPeer.Send(_dataWriter, deliveryMethod);
            return true;
        }
        #endregion

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
                _queuedAudio.ClearBuffer();
            }

            _isDisposed = true;
        }
        #endregion
    }
}