using System;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Backends;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;

namespace VoiceCraft.Network;

public class McApiServer : IDisposable
{
    public static readonly Version Version = new(Constants.Major, Constants.Minor, Constants.Patch);
    private readonly McApiNetworkBackend _networkBackend;
    private readonly VoiceCraftWorld _world;
    private bool _disposed;
    
    public uint MaxClients { get; set; }
    public string LoginToken { get; set; } = string.Empty;
    
    public McApiServer(McApiNetworkBackend networkBackend, VoiceCraftWorld world)
    {
        _networkBackend = networkBackend;
        _world = world;
        _networkBackend.OnLoginRequest += NetworkBackendOnLoginRequest;
        _networkBackend.OnNetworkReceive += NetworkBackendOnNetworkReceive;
    }

    ~McApiServer()
    {
        Dispose(false);
    }
    
    public async Task StartAsync(int port, uint maxClients)
    {
        await StopAsync();
        MaxClients = maxClients;
        await Task.Run(() => _networkBackend.Start(port));
    }

    public void Update()
    {
        if (!_networkBackend.IsStarted) return;
        _networkBackend.Update();
    }

    public async Task StopAsync(string? reason = null)
    {
        if (!_networkBackend.IsStarted) return;
        await Task.Run(() =>
        {
            _networkBackend.DisconnectAll(reason);
            _networkBackend.Stop();
        });
    }
    
    public void SendPacket<T>(McApiNetPeer netPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IMcApiPacket
    {
        if (!_networkBackend.IsStarted) return;
        try
        {
            _networkBackend.SendPacket(netPeer, packet, deliveryMethod);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public void Broadcast<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IMcApiPacket
    {
        if (!_networkBackend.IsStarted) return;
        try
        {
            _networkBackend.Broadcast(packet, deliveryMethod);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    //Event Handling
    private void NetworkBackendOnLoginRequest(McApiNetPeer netPeer)
    {
        try
        {
            if (!string.IsNullOrEmpty(LoginToken) && LoginToken != netPeer.LoginToken)
            {
                _networkBackend.Reject(netPeer, "VcMcApi.DisconnectReason.InvalidLoginToken");
                return;
            }

            if (netPeer.Version.Major != Version.Major || netPeer.Version.Minor != Version.Minor)
            {
                _networkBackend.Reject(netPeer, "VcMcApi.DisconnectReason.IncompatibleVersion");
                return;
            }

            if (_networkBackend.ConnectedPeersCount >= MaxClients)
            {
                _networkBackend.Reject(netPeer, "VcMcApi.DisconnectReason.ServerFull");
                return;
            }
            
            netPeer.Accept();
        }
        catch
        {
            if (netPeer.ConnectionState == McApiConnectionState.LoginRequested)
            {
                _networkBackend.Reject(netPeer, "VcMcApi.DisconnectReason.Error");
                return;
            }

            _networkBackend.Disconnect(netPeer, "VcMcApi.DisconnectReason.Error");
            throw; //This will go up the stack until it reaches a logger.
        }
    }

    private static void NetworkBackendOnNetworkReceive(McApiNetPeer netPeer, IMcApiPacket packet)
    {
        switch (packet)
        {
        }
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _networkBackend.Dispose();
            
            _networkBackend.OnLoginRequest -= NetworkBackendOnLoginRequest;
            _networkBackend.OnNetworkReceive -= NetworkBackendOnNetworkReceive;
        }
        
        _disposed = true;
    }
}