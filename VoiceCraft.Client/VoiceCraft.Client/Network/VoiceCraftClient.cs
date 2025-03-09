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
        public event Action<string, uint, bool, PositioningType>? OnInfoReceived;
        
        //Client Events
        public event Action<Entity>? OnEntityCreated;
        public event Action<Entity>? OnEntityDestroyed;
        
        //Public Properties
        public EventBasedNetListener Listener { get; }
        public EntityStore World { get; }
        public uint LocalEntityId { get; internal set; }
        public NetPeer? ServerPeer { get; internal set; }
        
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter;
        private readonly NetworkEventHandler _networkEventHandler;
        private readonly PacketEventHandler _packetHandler;
        private readonly WorldEventHandler _worldEventHandler;
        
        private readonly OpusEncoder _encoder;
        private readonly BufferedWaveProvider _queuedAudio;
        private readonly byte[] _extractedAudioBuffer;
        private readonly byte[] _encodedAudioBuffer;
        private bool _isDisposed;
        private uint _currentTimestamp;

        public VoiceCraftClient()
        {
            _dataWriter = new NetDataWriter();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            World = new EntityStore();
            _networkEventHandler = new NetworkEventHandler(this);
            _packetHandler = new PacketEventHandler(this);
            _worldEventHandler = new WorldEventHandler(this);
            
            _encoder = new OpusEncoder(WaveFormat.SampleRate, WaveFormat.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            _queuedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = false, BufferDuration = TimeSpan.FromSeconds(2) }; //2 seconds.
            _extractedAudioBuffer = new byte[BytesPerFrame];
            _encodedAudioBuffer = new byte[1000];
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
            if (_queuedAudio.BufferedBytes < BytesPerFrame) return;
            Array.Clear(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            Array.Clear(_encodedAudioBuffer, 0, _encodedAudioBuffer.Length);
            _queuedAudio.Read(_extractedAudioBuffer, 0, _extractedAudioBuffer.Length);
            _currentTimestamp += AudioConstants.SamplesPerFrame;
            //Turns out RTPIncrement is samples per frame. IDK how this shit works...
            var encodedBytes = _encoder.Encode(_extractedAudioBuffer, AudioConstants.SamplesPerFrame, _encodedAudioBuffer, _encodedAudioBuffer.Length);
            //We don't need to input our public ID.
            SendPacket(new AudioPacket() { Timestamp = _currentTimestamp, Data = _encodedAudioBuffer, DataLength = encodedBytes },
                DeliveryMethod.Unreliable);
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

        public void WriteAudio(byte[] buffer, int length)
        {
            _queuedAudio.AddSamples(buffer, 0, length);
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