using System;
using System.Net;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using OpusSharp.Core;
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
        private readonly OpusEncoder _encoder = new(Constants.SampleRate, Constants.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        
        //Buffers
        private readonly NetDataWriter _dataWriter = new();
        private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];
        private DateTime _lastAudioPeakTime = DateTime.MinValue;
        
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
            _netManager.Start();

            Listener.PeerConnectedEvent += peer =>
            {
                if (!Equals(peer, _serverPeer)) return;
                OnConnected?.Invoke();
            };

            Listener.PeerDisconnectedEvent += (peer, info) =>
            {
                if (!Equals(peer, _serverPeer)) return;
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
            // if (ConnectionState == ConnectionState.Disconnected) return; //Not connected.
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public void Write(byte[] buffer, int bytesRead)
        {
            
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