using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Servers;

public class LiteNetVoiceCraftServer : VoiceCraftServer
{
    private LiteNetVoiceCraftConfig _config = new();
    private readonly ConcurrentDictionary<NetPeer, LiteNetVoiceCraftNetPeer> _netPeers = new();
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly NetDataWriter _writer;

    public LiteNetVoiceCraftConfig Config
    {
        get => _config;
        set
        {
            if (_netManager.IsRunning)
                throw new InvalidOperationException();
            _config = value;
        }
    }

    public override PositioningType PositioningType => _config.PositioningType;
    public override string Motd => _config.Motd;
    public override uint MaxClients => _config.MaxClients;
    public override int ConnectedPeers => _netPeers.Count;

    public LiteNetVoiceCraftServer(VoiceCraftWorld world) : base(world)
    {
        _listener = new EventBasedNetListener();
        _writer = new NetDataWriter();
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };

        _listener.ConnectionRequestEvent += ConnectionRequestEvent;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += NetworkReceiveUnconnectedEvent;
        _listener.PeerDisconnectedEvent += PeerDisconnectedEvent;
    }

    public override void Start()
    {
        Stop();
        if (!_netManager.Start((int)_config.Port))
            throw new SocketException();
    }

    public override void Update()
    {
        _netManager.PollEvents();
    }

    public override void Stop()
    {
        if (!_netManager.IsRunning) return;
        DisconnectAll("VoiceCraft.DisconnectReason.Shutdown");
        _netManager.Stop();
        _netPeers.Clear();
    }

    public override void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet)
    {
        if (!_netManager.IsRunning) return;
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            _netManager.SendUnconnectedMessage(_writer, endPoint);
        }
    }

    public override void SendPacket<T>(VoiceCraftNetPeer vcNetPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        if (!_netManager.IsRunning ||
            vcNetPeer.Server != this ||
            vcNetPeer is not LiteNetVoiceCraftNetPeer liteNetPeer) return;
        var method = deliveryMethod switch
        {
            VcDeliveryMethod.Unreliable => DeliveryMethod.Unreliable,
            _ => DeliveryMethod.ReliableOrdered
        };
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            liteNetPeer.NetPeer.Send(_writer, method);
        }
    }

    public override void Broadcast<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer?[] excludes)
    {
        if (!_netManager.IsRunning) return;
        var method = deliveryMethod switch
        {
            VcDeliveryMethod.Unreliable => DeliveryMethod.Unreliable,
            _ => DeliveryMethod.ReliableOrdered
        };
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            foreach (var netPeer in _netPeers.Values)
            {
                if (excludes.Contains(netPeer)) continue;
                netPeer.NetPeer.Send(_writer, method);
            }
        }
    }

    public override void Disconnect(VoiceCraftNetPeer vcNetPeer, string reason, bool force = false)
    {
        if (!_netManager.IsRunning ||
            vcNetPeer.Server != this ||
            vcNetPeer is not LiteNetVoiceCraftNetPeer liteNetPeer) return;

        var logoutPacket = PacketPool<VcLogoutRequestPacket>.GetPacket(() => new VcLogoutRequestPacket());
        try
        {
            logoutPacket.Set(reason);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                if (force)
                {
                    _netManager.DisconnectPeerForce(liteNetPeer.NetPeer);
                    return;
                }

                liteNetPeer.NetPeer.Disconnect(_writer);
            }
        }
        finally
        {
            logoutPacket.Return();
        }
    }

    public override void DisconnectAll(string? reason = null)
    {
        if (!_netManager.IsRunning) return;
        if (reason == null)
        {
            _netManager.DisconnectAll();
            return;
        }

        var logoutPacket = PacketPool<VcLogoutRequestPacket>.GetPacket(() => new VcLogoutRequestPacket());
        try
        {
            logoutPacket.Set(reason);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                _netManager.DisconnectAll(_writer.Data, 0, _writer.Length);
            }
        }
        finally
        {
            logoutPacket.Return();
        }
    }

    protected override void AcceptRequest(VcLoginRequestPacket packet, object? data)
    {
        if (data is not ConnectionRequest request) return;
        var peer = request.Accept();
        var liteNetPeer = new LiteNetVoiceCraftNetPeer(
            this,
            peer,
            packet.UserGuid,
            packet.ServerUserGuid,
            packet.Locale,
            packet.PositioningType);
        var acceptPacket = PacketPool<VcAcceptResponsePacket>.GetPacket(() => new VcAcceptResponsePacket());
        try
        {
            if (!_netPeers.TryAdd(peer, liteNetPeer))
                throw new Exception();
            var id = World.GetNextId();
            var entity = new VoiceCraftNetworkEntity(liteNetPeer, id);
            liteNetPeer.Tag = entity;
            
            World.AddEntity(entity);
            entity.Name = "New Client";
            acceptPacket.Set(packet.RequestId);
            SendPacket(liteNetPeer, acceptPacket);
        }
        catch
        {
            Disconnect(liteNetPeer, "VoiceCraft.DisconnectReason.Error");
        }
        finally
        {
            acceptPacket.Return();
        }
    }

    protected override void RejectRequest(VcLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not ConnectionRequest request) return;
        var denyPacket = PacketPool<VcDenyResponsePacket>.GetPacket(() => new VcDenyResponsePacket());
        try
        {
            denyPacket.Set(packet.RequestId, reason);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)denyPacket.PacketType);
                _writer.Put(denyPacket);
                request.Reject(_writer);
            }
        }
        finally
        {
            denyPacket.Return();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        base.Dispose(disposing);
        if (!disposing) return;
        _listener.ConnectionRequestEvent -= ConnectionRequestEvent;
        _listener.NetworkReceiveEvent -= NetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent -= NetworkReceiveUnconnectedEvent;
        _listener.PeerDisconnectedEvent -= PeerDisconnectedEvent;
    }

    #region LiteNetLib Events

    private void ConnectionRequestEvent(ConnectionRequest request)
    {
        try
        {
            ProcessPacket(request.Data, packet => { ExecutePacket(packet, request); });
        }
        catch
        {
            request.Reject();
        }
    }

    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            ProcessPacket(reader, packet =>
            {
                if (!_netPeers.TryGetValue(peer, out var vcPeer)) return;
                ExecutePacket(packet, vcPeer);
            });
        }
        catch
        {
            //Do Nothing
        }
    }

    private void NetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        try
        {
            ProcessUnconnectedPacket(reader, packet => { ExecutePacket(packet, remoteEndPoint); });
        }
        catch
        {
            //Do Nothing
        }
    }

    private void PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!_netPeers.TryRemove(peer, out var vcPeer)) return;
        if (vcPeer.Tag is not VoiceCraftNetworkEntity networkEntity || networkEntity.Destroyed) return;
        World.DestroyEntity(networkEntity.Id);
    }

    #endregion

    public class LiteNetVoiceCraftConfig
    {
        public string Language { get; set; } = Constants.DefaultLanguage;
        public uint Port { get; set; } = 9050;
        public uint ExternalPort { get; set; }
        public uint PortMappingLifetimeMinutes { get; set; } = 60;
        public uint PortMappingTimeoutSeconds { get; set; } = 5;
        public uint MaxClients { get; set; } = 100;
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public PositioningType PositioningType { get; set; } = PositioningType.Server;

        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool AutoOpenPort { get; set; }

        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool EnableVisibilityDisplay { get; set; } = true;
    }
}