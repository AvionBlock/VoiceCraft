using System;
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
    public class VoiceCraftClient : IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);
        public static readonly WaveFormat WaveFormat = new(Constants.SampleRate, Constants.Channels);
        public static readonly uint BytesPerFrame = (uint)WaveFormat.ConvertLatencyToByteSize(Constants.FrameSizeMs);
        
        //Network Events
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        
        //Public Properties
        public ConnectionState ConnectionState => ServerPeer?.ConnectionState ?? ConnectionState.Disconnected;
        public EventBasedNetListener Listener { get; } = new();
        public NetDataWriter DataWriter { get; } = new();
        public NetPeer? ServerPeer { get; private set; }
        public NetworkSystem NetworkSystem { get; }
        
        private readonly NetManager _netManager;
        private readonly OpusEncoder _encoder;
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
            
            _encoder = new OpusEncoder(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            
            _netManager.Start();

            Listener.PeerConnectedEvent += (peer) =>
            {
                if(Equals(peer, ServerPeer))
                    OnConnected?.Invoke();
            };
            
            Listener.PeerDisconnectedEvent += (peer, info) =>
            {
                if (Equals(peer, ServerPeer))
                    OnDisconnected?.Invoke(info);
            };
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        public bool Ping(string ip, uint port)
        {
            var packet = new InfoPacket() { Tick = Environment.TickCount };
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
            if(ConnectionState != ConnectionState.Disconnected)
                throw new InvalidOperationException("This client is already connected or is connecting to a server!");
            
            DataWriter.Reset();
            var loginPacket = new LoginPacket()
            {
                Version = Version.ToString(),
                LoginType = loginType,
                PositioningType = PositioningType.Server
            };
            loginPacket.Serialize(DataWriter);
            var serverPeer = _netManager.Connect(ip, port, DataWriter);
            ServerPeer = serverPeer ?? throw new InvalidOperationException("A connection request is awaiting!");
        }

        public void Update()
        {
            _netManager.PollEvents();
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
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
            }

            _isDisposed = true;
        }
        #endregion
    }
}