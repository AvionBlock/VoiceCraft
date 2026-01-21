using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Servers;

public class LiteNetVoiceCraftServer : VoiceCraftServer
{
    private readonly ConcurrentDictionary<NetPeer, VoiceCraftNetPeer> _netPeers = new();
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly NetDataWriter _writer;

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

    public override void Start(int port)
    {
        Stop();
        if (!_netManager.Start(port))
            throw new SocketException();
    }

    public override void Update()
    {
        _netManager.PollEvents();
    }

    public override void Stop()
    {
        if (!_netManager.IsRunning) return;
        _netManager.Stop();
        _netPeers.Clear();
    }

    public override void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet)
    {
        if (!_netManager.IsRunning) return;
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                _netManager.SendUnconnectedMessage(_writer, endPoint);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void SendPacket<T>(VoiceCraftNetPeer vcNetPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        if (!_netManager.IsRunning || vcNetPeer is not LiteNetVoiceCraftNetPeer liteNetPeer) return;
        var method = deliveryMethod switch
        {
            VcDeliveryMethod.Unreliable => DeliveryMethod.Unreliable,
            _ => DeliveryMethod.ReliableOrdered
        };
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                liteNetPeer.NetPeer.Send(_writer, method);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
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
        try
        {
            lock (_writer)
            {
                var networkEntities = World.Entities.OfType<VoiceCraftNetworkEntity>()
                    .Where(x => x.NetPeer is LiteNetVoiceCraftNetPeer);
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                foreach (var networkEntity in networkEntities)
                {
                    if (networkEntity.NetPeer is not LiteNetVoiceCraftNetPeer liteNetPeer) continue;
                    if (excludes.Contains(networkEntity.NetPeer)) continue;
                    liteNetPeer.NetPeer.Send(_writer, method);
                }
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    protected override void AcceptRequest(VcLoginRequestPacket packet, object? data)
    {
        if (data is not ConnectionRequest request) return;
        var peer = request.Accept();
        var liteNetPeer = new LiteNetVoiceCraftNetPeer(peer, packet.UserGuid, packet.ServerUserGuid, packet.Locale,
            packet.PositioningType);
        _netPeers.TryAdd(peer, liteNetPeer);
        try
        {
            var id = World.GetNextId();
            var entity = new VoiceCraftNetworkEntity(liteNetPeer, id);
            liteNetPeer.Tag = entity;
            World.AddEntity(entity);
            SendPacket(liteNetPeer, PacketPool<VcAcceptResponsePacket>.GetPacket().Set(packet.RequestId));
        }
        catch
        {
            Disconnect(liteNetPeer, "VoiceCraft.DisconnectReason.Error");
        }
    }

    protected override void RejectRequest(VcLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not ConnectionRequest request) return;
        var responsePacket = PacketPool<VcDenyResponsePacket>.GetPacket().Set(packet.RequestId, reason);
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)responsePacket.PacketType);
                _writer.Put(responsePacket);
                request.Reject(_writer);
            }
        }
        finally
        {
            PacketPool<VcDenyResponsePacket>.Return(responsePacket);
        }
    }

    protected override void Disconnect(VoiceCraftNetPeer vcNetPeer, string reason)
    {
        if (vcNetPeer is not LiteNetVoiceCraftNetPeer liteNetPeer) return;
        var logoutPacket = PacketPool<VcLogoutRequestPacket>.GetPacket().Set(reason);
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)logoutPacket.PacketType);
                _writer.Put(logoutPacket);
                liteNetPeer.NetPeer.Disconnect(_writer);
            }
        }
        finally
        {
            PacketPool<VcLogoutRequestPacket>.Return(logoutPacket);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            _netManager.Stop();
            _netPeers.Clear();

            _listener.ConnectionRequestEvent -= ConnectionRequestEvent;
            _listener.NetworkReceiveEvent -= NetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent -= NetworkReceiveUnconnectedEvent;
            _listener.PeerDisconnectedEvent -= PeerDisconnectedEvent;
        }

        base.Dispose(disposing);
    }

    #region LiteNetLib Events

    private void ConnectionRequestEvent(ConnectionRequest request)
    {
        ProcessPacket(request.Data, packet => { ExecutePacket(packet, request); });
    }

    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        ProcessPacket(reader, packet =>
        {
            if (!_netPeers.TryGetValue(peer, out var vcPeer)) return;
            ExecutePacket(packet, vcPeer.Tag);
        });
    }

    private void NetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        ProcessUnconnectedPacket(reader, packet => { ExecutePacket(packet, remoteEndPoint); });
    }

    private void PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!_netPeers.TryRemove(peer, out var vcPeer)) return;
        if (vcPeer.Tag is not VoiceCraftNetworkEntity networkEntity || networkEntity.Destroyed) return;
        World.DestroyEntity(networkEntity.Id);
    }

    #endregion
}