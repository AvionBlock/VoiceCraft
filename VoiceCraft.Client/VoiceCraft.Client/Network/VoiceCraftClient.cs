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

        //Network Events
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;

        //Public Properties
        public ConnectionState ConnectionState => ServerPeer?.ConnectionState ?? ConnectionState.Disconnected;
        public EventBasedNetListener Listener { get; } = new();
        public NetDataWriter DataWriter { get; } = new();
        public NetPeer? ServerPeer { get; private set; }
        public VoiceCraftWorld World { get; } = new();
        public NetworkSystem NetworkSystem { get; }
        public EntityAudioBufferSystem AudioBufferSystem { get; }
        
        private readonly NetManager _netManager;
        private readonly OpusEncoder _encoder = new(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        private readonly byte[] _senderBuffer = new byte[Constants.BytesPerFrame];
        private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];
        private readonly BufferedWaveProvider _sendBuffer = new(WaveFormat) { DiscardOnBufferOverflow = true};
        private readonly BufferedWaveProvider _recvBuffer = new(WaveFormat) { ReadFully = true, DiscardOnBufferOverflow = true };
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
            AudioBufferSystem = new EntityAudioBufferSystem(this);
            _netManager.Start();

            Listener.PeerConnectedEvent += peer =>
            {
                if (!Equals(peer, ServerPeer)) return;
                OnConnected?.Invoke();
            };

            Listener.PeerDisconnectedEvent += (peer, info) =>
            {
                if (!Equals(peer, ServerPeer)) return;
                try
                {
                    var reason = !info.AdditionalData.IsNull ? info.AdditionalData.GetString() : info.Reason.ToString();
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

            DataWriter.Reset();
            var loginPacket = new LoginPacket(Version.ToString(), loginType);
            loginPacket.Serialize(DataWriter);
            var serverPeer = _netManager.Connect(ip, port, DataWriter);
            ServerPeer = serverPeer ?? throw new InvalidOperationException("A connection request is awaiting!");
        }

        public void Update()
        {
            _netManager.PollEvents();
            if (ServerPeer == null) return; //Not connected.
            
            while (_sendBuffer.BufferedBytes >= Constants.BytesPerFrame)
            {
                Array.Clear(_senderBuffer);
                var read = _sendBuffer.Read(_senderBuffer, 0, _senderBuffer.Length);
                var encoded = _encoder.Encode(_senderBuffer, read, _encodeBuffer, _encodeBuffer.Length);
                var packet = new AudioPacket(ServerPeer.RemoteId, _senderBuffer, encoded, _timestamp += Constants.SamplesPerFrame);
                NetworkSystem.SendPacket(packet);
            }

            foreach (var entity in World.Entities)
            {
                var buffer = new byte[Constants.BytesPerFrame];
                AudioBufferSystem.GetNextFrame(entity.Value, buffer);
            }
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            _netManager.DisconnectAll();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _sendBuffer.AddSamples(buffer, offset, count);
        }

        public void Read(byte[] buffer, int offset, int count)
        {
            _recvBuffer.Read(buffer, offset, count);
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
                AudioBufferSystem.Dispose();
            }

            _isDisposed = true;
        }
        #endregion
    }
}