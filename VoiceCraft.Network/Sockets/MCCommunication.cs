using System.Collections.Concurrent;
using System.Net;
using System.Text;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Network.Sockets;

using System.Collections.ObjectModel;

/// <summary>
/// Minecraft Communication server using HTTP for server-side plugin communication.
/// Handles authentication, session management, and player updates.
/// </summary>
public class MCCommunication : Disposable
{
#pragma warning disable CA1031 // Do not catch general exception types
    #region Constants and Properties
    /// <summary>
    /// Protocol version - matches Core.Constants.Version.
    /// </summary>
    public const string Version = Core.Constants.Version;
    
    /// <summary>
    /// Maximum request body size in bytes (1MB).
    /// </summary>
    private const int MaxRequestSize = 1024 * 1024;

    private HttpListener _webServer = new();
    
    /// <summary>
    /// Gets the login key required for authentication.
    /// </summary>
    public string LoginKey { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the packet registry for serialization.
    /// </summary>
    public PacketRegistry PacketRegistry { get; } = new();
    
    /// <summary>
    /// Gets the active sessions by token.
    /// </summary>
    public ConcurrentDictionary<string, long> Sessions { get; } = new();
            
    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 8000;
    
    private Task? _activityChecker;
    private CancellationTokenSource? _cts;
    #endregion

    #region Debug Properties
    /// <summary>Gets or sets whether to log exceptions.</summary>
    public bool LogExceptions { get; set; }
    
    /// <summary>Gets or sets whether to log inbound packets.</summary>
    public bool LogInbound { get; set; }
    
    /// <summary>Gets or sets whether to log outbound packets.</summary>
    public bool LogOutbound { get; set; }
    
    /// <summary>Gets or sets the filter for inbound packet logging.</summary>
    public Collection<MCCommPacketTypes> InboundFilter { get; } = [];
    
    /// <summary>Gets or sets the filter for outbound packet logging.</summary>
    public Collection<MCCommPacketTypes> OutboundFilter { get; } = [];
    #endregion

    #region Events
    /// <summary>Raised when the server starts.</summary>
    public event EventHandler? OnStarted;
    
    /// <summary>Raised when the server stops.</summary>
    public event EventHandler<MCStoppedEventArgs>? OnStopped;
    
    /// <summary>Raised when a Minecraft server connects.</summary>
    public event EventHandler<ServerConnectedEventArgs>? OnServerConnected;
    
    /// <summary>Raised when a Minecraft server disconnects.</summary>
    public event EventHandler<ServerDisconnectedEventArgs>? OnServerDisconnected;
    
    public event EventHandler<PacketEventArgs<Login>>? OnLoginReceived;
    public event EventHandler<PacketEventArgs<Logout>>? OnLogoutReceived;
    public event EventHandler<PacketEventArgs<Accept>>? OnAcceptReceived;
    public event EventHandler<PacketEventArgs<Deny>>? OnDenyReceived;
    public event EventHandler<PacketEventArgs<Bind>>? OnBindReceived;
    public event EventHandler<PacketEventArgs<Update>>? OnUpdateReceived;
    public event EventHandler<PacketEventArgs<AckUpdate>>? OnAckUpdateReceived;
    public event EventHandler<PacketEventArgs<GetChannels>>? OnGetChannelsReceived;
    public event EventHandler<PacketEventArgs<GetChannelSettings>>? OnGetChannelSettingsReceived;
    public event EventHandler<PacketEventArgs<SetChannelSettings>>? OnSetChannelSettingsReceived;
    public event EventHandler<PacketEventArgs<GetDefaultSettings>>? OnGetDefaultSettingsReceived;
    public event EventHandler<PacketEventArgs<SetDefaultSettings>>? OnSetDefaultSettingsReceived;
    public event EventHandler<PacketEventArgs<GetParticipants>>? OnGetParticipantsReceived;
    public event EventHandler<PacketEventArgs<DisconnectParticipant>>? OnDisconnectParticipantReceived;
    public event EventHandler<PacketEventArgs<GetParticipantBitmask>>? OnGetParticipantBitmaskReceived;
    public event EventHandler<PacketEventArgs<SetParticipantBitmask>>? OnSetParticipantBitmaskReceived;
    public event EventHandler<PacketEventArgs<MuteParticipant>>? OnMuteParticipantReceived;
    public event EventHandler<PacketEventArgs<UnmuteParticipant>>? OnUnmuteParticipantReceived;
    public event EventHandler<PacketEventArgs<DeafenParticipant>>? OnDeafenParticipantReceived;
    public event EventHandler<PacketEventArgs<UndeafenParticipant>>? OnUndeafenParticipantReceived;
    public event EventHandler<PacketEventArgs<ANDModParticipantBitmask>>? OnANDModParticipantBitmaskReceived;
    public event EventHandler<PacketEventArgs<ORModParticipantBitmask>>? OnORModParticipantBitmaskReceived;
    public event EventHandler<PacketEventArgs<XORModParticipantBitmask>>? OnXORModParticipantBitmaskReceived;
    public event EventHandler<PacketEventArgs<ChannelMove>>? OnChannelMoveReceived;

    /// <summary>Raised when a packet is received (debug).</summary>
    public event EventHandler<PacketEventArgs<MCCommPacket>>? OnInboundPacket;
    
    /// <summary>Raised when a packet is sent (debug).</summary>
    public event EventHandler<PacketEventArgs<MCCommPacket>>? OnOutboundPacket;
    
    /// <summary>Raised when an exception occurs.</summary>
    public event EventHandler<MCErrorEventArgs>? OnExceptionError;
    
    /// <summary>Raised when starting fails.</summary>
    public event EventHandler<MCErrorEventArgs>? OnFailed;
    #endregion

    /// <summary>
    /// Initializes a new instance of the MCCommunication class.
    /// </summary>
    public MCCommunication()
    {
        RegisterPackets();
    }

    private void RegisterPackets()
    {
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Login, typeof(Login));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Logout, typeof(Logout));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Accept, typeof(Accept));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Deny, typeof(Deny));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Bind, typeof(Bind));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Update, typeof(Update));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.AckUpdate, typeof(AckUpdate));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetChannels, typeof(GetChannels));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetChannelSettings, typeof(GetChannelSettings));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetChannelSettings, typeof(SetChannelSettings));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetDefaultSettings, typeof(GetDefaultSettings));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetDefaultSettings, typeof(SetDefaultSettings));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetParticipants, typeof(GetParticipants));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.DisconnectParticipant, typeof(DisconnectParticipant));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetParticipantBitmask, typeof(GetParticipantBitmask));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetParticipantBitmask, typeof(SetParticipantBitmask));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.MuteParticipant, typeof(MuteParticipant));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.UnmuteParticipant, typeof(UnmuteParticipant));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.DeafenParticipant, typeof(DeafenParticipant));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.UndeafenParticipant, typeof(UndeafenParticipant));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ANDModParticipantBitmask, typeof(ANDModParticipantBitmask));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ORModParticipantBitmask, typeof(ORModParticipantBitmask));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.XORModParticipantBitmask, typeof(XORModParticipantBitmask));
        PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ChannelMove, typeof(ChannelMove));
    }

    /// <summary>
    /// Starts the HTTP server on the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="loginKey">The login key for authentication.</param>
    public async Task StartAsync(ushort port, string loginKey)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        
        try
        {
            LoginKey = loginKey;
            _cts = new CancellationTokenSource();
            _webServer.Prefixes.Add($"http://*:{port}/");
            _webServer.Start();
            
            OnLoginReceived += HandleLoginReceived;
            OnLogoutReceived += HandleLogoutReceived;
            
            _activityChecker = Task.Run(ActivityCheckAsync);
            OnStarted?.Invoke(this, EventArgs.Empty);
            
            await ListenAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnFailed?.Invoke(this, new MCErrorEventArgs(ex));
        }
    }

    /// <summary>
    /// Stops the HTTP server and cleans up resources.
    /// </summary>
    /// <param name="reason">Optional reason for stopping.</param>
    public void Stop(string? reason = null)
    {
        if (!_webServer.IsListening) 
            return;
            
        _cts?.Cancel();
        _webServer.Stop();
        
        OnLoginReceived -= HandleLoginReceived;
        OnLogoutReceived -= HandleLogoutReceived;
        
        Sessions.Clear();
        _activityChecker = null;
        
        OnStopped?.Invoke(this, new MCStoppedEventArgs(reason));
    }

    /// <summary>
    /// Sends a response packet to the client.
    /// </summary>
    /// <param name="ctx">The HTTP listener context.</param>
    /// <param name="code">The HTTP status code.</param>
    /// <param name="packet">The packet to send.</param>
    public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, MCCommPacket packet)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(packet);
                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
            OnOutboundPacket?.Invoke(this, new PacketEventArgs<MCCommPacket>(packet, ctx));

        var content = Encoding.UTF8.GetBytes(packet.SerializePacket());
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";
        ctx.Response.OutputStream.Write(content, 0, content.Length);
        ctx.Response.OutputStream.Close();
    }

    private async Task ListenAsync()
    {
        while (_webServer.IsListening)
        {
            try
            {
                var ctx = await _webServer.GetContextAsync().ConfigureAwait(false);
                
                // Offload processing to prevent blocking the listener
                _ = Task.Run(() => ProcessRequestAsync(ctx));
            }
            catch (HttpListenerException)
            {
                return; // Server stopped
            }
            catch (ObjectDisposedException)
            {
                return; // Server disposed
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(this, new MCErrorEventArgs(ex));
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.HttpMethod != HttpMethod.Post.Method)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                ctx.Response.Close();
                return;
            }

            // Security: Limit request size to prevent DoS
            if (ctx.Request.ContentLength64 > MaxRequestSize)
            {
                SendResponse(ctx, HttpStatusCode.RequestEntityTooLarge, new Deny { Reason = "Request too large" });
                return;
            }

            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Empty body" });
                return;
            }

            var packet = PacketRegistry.GetPacketFromJsonString(content);

            if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                OnInboundPacket?.Invoke(this, new PacketEventArgs<MCCommPacket>(packet, ctx));

            HandlePacket(packet, ctx);
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                OnExceptionError?.Invoke(this, new MCErrorEventArgs(ex));

            try
            {
                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Invalid Data!" });
            }
            catch
            {
                // Context might be closed already
            }
        }
    }

    private void HandlePacket(MCCommPacket packet, HttpListenerContext ctx)
    {
        try
        {
            // Validate session for non-login packets
            if (packet.PacketId != (byte)MCCommPacketTypes.Login)
            {
                if (Sessions.TryGetValue(packet.Token, out var session))
                {
                    Sessions.TryUpdate(packet.Token, Environment.TickCount64, session);
                }
                else
                {
                    SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Token!" });
                    return;
                }
            }

            DispatchPacket(packet, ctx);
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                OnExceptionError?.Invoke(this, new MCErrorEventArgs(ex));

            SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Invalid Data!" });
        }
    }

    private void DispatchPacket(MCCommPacket packet, HttpListenerContext ctx)
    {
        switch ((MCCommPacketTypes)packet.PacketId)
        {
            case MCCommPacketTypes.Login:
                OnLoginReceived?.Invoke(this, new PacketEventArgs<Login>((Login)packet, ctx));
                break;
            case MCCommPacketTypes.Logout:
                OnLogoutReceived?.Invoke(this, new PacketEventArgs<Logout>((Logout)packet, ctx));
                break;
            case MCCommPacketTypes.Accept:
                OnAcceptReceived?.Invoke(this, new PacketEventArgs<Accept>((Accept)packet, ctx));
                break;
            case MCCommPacketTypes.Deny:
                OnDenyReceived?.Invoke(this, new PacketEventArgs<Deny>((Deny)packet, ctx));
                break;
            case MCCommPacketTypes.Bind:
                OnBindReceived?.Invoke(this, new PacketEventArgs<Bind>((Bind)packet, ctx));
                break;
            case MCCommPacketTypes.Update:
                OnUpdateReceived?.Invoke(this, new PacketEventArgs<Update>((Update)packet, ctx));
                break;
            case MCCommPacketTypes.GetChannels:
                OnGetChannelsReceived?.Invoke(this, new PacketEventArgs<GetChannels>((GetChannels)packet, ctx));
                break;
            case MCCommPacketTypes.AckUpdate:
                OnAckUpdateReceived?.Invoke(this, new PacketEventArgs<AckUpdate>((AckUpdate)packet, ctx));
                break;
            case MCCommPacketTypes.GetChannelSettings:
                OnGetChannelSettingsReceived?.Invoke(this, new PacketEventArgs<GetChannelSettings>((GetChannelSettings)packet, ctx));
                break;
            case MCCommPacketTypes.SetChannelSettings:
                OnSetChannelSettingsReceived?.Invoke(this, new PacketEventArgs<SetChannelSettings>((SetChannelSettings)packet, ctx));
                break;
            case MCCommPacketTypes.GetDefaultSettings:
                OnGetDefaultSettingsReceived?.Invoke(this, new PacketEventArgs<GetDefaultSettings>((GetDefaultSettings)packet, ctx));
                break;
            case MCCommPacketTypes.SetDefaultSettings:
                OnSetDefaultSettingsReceived?.Invoke(this, new PacketEventArgs<SetDefaultSettings>((SetDefaultSettings)packet, ctx));
                break;
            case MCCommPacketTypes.GetParticipants:
                OnGetParticipantsReceived?.Invoke(this, new PacketEventArgs<GetParticipants>((GetParticipants)packet, ctx));
                break;
            case MCCommPacketTypes.DisconnectParticipant:
                OnDisconnectParticipantReceived?.Invoke(this, new PacketEventArgs<DisconnectParticipant>((DisconnectParticipant)packet, ctx));
                break;
            case MCCommPacketTypes.GetParticipantBitmask:
                OnGetParticipantBitmaskReceived?.Invoke(this, new PacketEventArgs<GetParticipantBitmask>((GetParticipantBitmask)packet, ctx));
                break;
            case MCCommPacketTypes.SetParticipantBitmask:
                OnSetParticipantBitmaskReceived?.Invoke(this, new PacketEventArgs<SetParticipantBitmask>((SetParticipantBitmask)packet, ctx));
                break;
            case MCCommPacketTypes.MuteParticipant:
                OnMuteParticipantReceived?.Invoke(this, new PacketEventArgs<MuteParticipant>((MuteParticipant)packet, ctx));
                break;
            case MCCommPacketTypes.UnmuteParticipant:
                OnUnmuteParticipantReceived?.Invoke(this, new PacketEventArgs<UnmuteParticipant>((UnmuteParticipant)packet, ctx));
                break;
            case MCCommPacketTypes.DeafenParticipant:
                OnDeafenParticipantReceived?.Invoke(this, new PacketEventArgs<DeafenParticipant>((DeafenParticipant)packet, ctx));
                break;
            case MCCommPacketTypes.UndeafenParticipant:
                OnUndeafenParticipantReceived?.Invoke(this, new PacketEventArgs<UndeafenParticipant>((UndeafenParticipant)packet, ctx));
                break;
            case MCCommPacketTypes.ANDModParticipantBitmask:
                OnANDModParticipantBitmaskReceived?.Invoke(this, new PacketEventArgs<ANDModParticipantBitmask>((ANDModParticipantBitmask)packet, ctx));
                break;
            case MCCommPacketTypes.ORModParticipantBitmask:
                OnORModParticipantBitmaskReceived?.Invoke(this, new PacketEventArgs<ORModParticipantBitmask>((ORModParticipantBitmask)packet, ctx));
                break;
            case MCCommPacketTypes.XORModParticipantBitmask:
                OnXORModParticipantBitmaskReceived?.Invoke(this, new PacketEventArgs<XORModParticipantBitmask>((XORModParticipantBitmask)packet, ctx));
                break;
            case MCCommPacketTypes.ChannelMove:
                OnChannelMoveReceived?.Invoke(this, new PacketEventArgs<ChannelMove>((ChannelMove)packet, ctx));
                break;
        }
    }

    private void HandleLoginReceived(object? sender, PacketEventArgs<Login> e)
    {
        var packet = e.Packet;
        var ctx = (HttpListenerContext)e.Source;

        if (packet.LoginKey != LoginKey)
        {
            SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Login Key!" });
            return;
        }
        
        if (packet.Version != Version)
        {
            SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = $"Version mismatch! Expected: {Version}, Got: {packet.Version}" });
            return;
        }
        
        var token = Guid.NewGuid().ToString();
        Sessions.TryAdd(token, Environment.TickCount64);
        SendResponse(ctx, HttpStatusCode.OK, new Accept { Token = token });
        OnServerConnected?.Invoke(this, new ServerConnectedEventArgs(token, ctx.Request.RemoteEndPoint.Address.ToString()));
    }

    private void HandleLogoutReceived(object? sender, PacketEventArgs<Logout> e)
    {
        var packet = e.Packet;
        var ctx = (HttpListenerContext)e.Source;
        
        if (Sessions.TryRemove(packet.Token, out _))
        {
            SendResponse(ctx, HttpStatusCode.OK, new Accept());
            OnServerDisconnected?.Invoke(this, new ServerDisconnectedEventArgs("Server disconnected gracefully", packet.Token));
            return;
        }
        
        SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Token!" });
    }

    private async Task ActivityCheckAsync()
    {
        while (_webServer.IsListening && _cts is { IsCancellationRequested: false })
        {
            foreach (var session in Sessions)
            {
                var elapsed = Environment.TickCount64 - session.Value;
                if (elapsed > Timeout && Sessions.TryRemove(session))
                {
                    OnServerDisconnected?.Invoke(this, new ServerDisconnectedEventArgs($"Server timed out after {elapsed}ms", session.Key));
                }
            }

            try
            {
                await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_webServer.IsListening)
                Stop("Disposed");
                
            _cts?.Dispose();
            _webServer.Close();
        }
        base.Dispose(disposing);
    }
}

public class ServerConnectedEventArgs : EventArgs
{
    public string Token { get; }
    public string Address { get; }
    public ServerConnectedEventArgs(string token, string address) { Token = token; Address = address; }
}

public class ServerDisconnectedEventArgs : EventArgs
{
    public string Reason { get; }
    public string Token { get; }
    public ServerDisconnectedEventArgs(string reason, string token) { Reason = reason; Token = token; }
}

public class MCStoppedEventArgs : EventArgs
{
    public string? Reason { get; }
    public MCStoppedEventArgs(string? reason) => Reason = reason;
}

public class MCErrorEventArgs : EventArgs
{
    public Exception Error { get; }
    public MCErrorEventArgs(Exception error) => Error = error;
}
