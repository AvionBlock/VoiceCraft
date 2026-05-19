using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using System.Runtime.InteropServices.JavaScript;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Client.Browser;

public class WebRtcVoiceCraftClient(
        Func<IAudioEncoder> audioEncoderFactory,
        Func<IAudioDecoder> decoderFactory,
        SettingsService settingsService)
    : VoiceCraftClient(audioEncoderFactory, decoderFactory)
{
    private static readonly SemaphoreSlim ConnectionLock = new(1, 1);
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();

    public override PositioningType PositioningType { get; } = PositioningType.Server;
    public override event Action? OnConnected;
    public override event Action<string?>? OnDisconnected;

    public override Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default)
    {
        return PingCoreAsync(ip, port, token);
    }

    public override async Task ConnectAsync(
        string ip,
        int port,
        Guid userGuid,
        Guid serverUserGuid,
        string locale,
        PositioningType positioningType)
    {
        if (ConnectionState != VcConnectionState.Disconnected) return;
        ConnectionState = VcConnectionState.Connecting;
        Reset();

        await ConnectionLock.WaitAsync();
        var requestId = Guid.NewGuid();
        try
        {
            await JsWebRtc.ConnectAsync(ToSignalingUrl(ip, port), GetIceServersJson());
            var packet = PacketPool<VcLoginRequestPacket>.GetPacket(() => new VcLoginRequestPacket())
                .Set(requestId, userGuid, serverUserGuid, locale, Version, positioningType);
            SendPacket(packet);

            _ = await GetResponseAsync<VcAcceptResponsePacket, Guid>(
                requestId,
                response => response.RequestId,
                TimeSpan.FromSeconds(8));
            ConnectionState = VcConnectionState.Connected;
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            await DisconnectAsync(ex.Message);
        }
        finally
        {
            ConnectionLock.Release();
        }
    }

    public override void Update()
    {
        while (JsWebRtc.Receive() is { } data)
        {
            lock (_reader)
            {
                _reader.Clear();
                _reader.SetSource(data);
                ProcessPacket(_reader, ExecutePacket);
            }
        }

        if (TryConsumeCloseReason(out var reason))
            _ = DisconnectAsync(reason);
    }

    public override Task DisconnectAsync(string? reason = null)
    {
        if (ConnectionState is VcConnectionState.Disconnected or VcConnectionState.Disconnecting)
            return Task.CompletedTask;

        ConnectionState = VcConnectionState.Disconnecting;
        JsWebRtc.Close();
        ConnectionState = VcConnectionState.Disconnected;
        OnDisconnected?.Invoke(reason);
        return Task.CompletedTask;
    }

    public override void SendUnconnectedPacket<T>(string ip, int port, T packet)
    {
        PacketPool<T>.Return(packet);
    }

    private async Task<ServerInfo> PingCoreAsync(string ip, int port, CancellationToken token)
    {
        if (ConnectionState != VcConnectionState.Disconnected)
            throw new InvalidOperationException("Cannot ping while a WebRTC connection is active.");

        await ConnectionLock.WaitAsync(token);
        try
        {
            await JsWebRtc.ConnectAsync(ToSignalingUrl(ip, port), GetIceServersJson());
            token.ThrowIfCancellationRequested();
            var packet = PacketPool<VcInfoRequestPacket>.GetPacket(() => new VcInfoRequestPacket())
                .Set(Environment.TickCount);
            SendPacket(packet);
            return await GetResponseAsync<VcInfoResponsePacket, ServerInfo>(
                Guid.Empty,
                response => new ServerInfo(response),
                TimeSpan.FromSeconds(8),
                token);
        }
        finally
        {
            JsWebRtc.Close();
            ConnectionLock.Release();
        }
    }

    public override void SendPacket<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                JsWebRtc.Send(_writer.CopyData());
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
            JsWebRtc.Close();
        base.Dispose(disposing);
    }

    private async Task<TResult> GetResponseAsync<TPacket, TResult>(
        Guid requestId,
        Func<TPacket, TResult> selector,
        TimeSpan timeout,
        CancellationToken token = default)
        where TPacket : IVoiceCraftPacket
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            token.ThrowIfCancellationRequested();

            var result = TryReadResponse<TPacket, TResult>(requestId, selector);
            if (result.Found)
                return result.Value!;

            if (TryConsumeCloseReason(out var closeReason))
                throw new InvalidOperationException(closeReason);

            await Task.Delay(1, token);
        }

        throw new TimeoutException();
    }

    private (bool Found, TResult? Value) TryReadResponse<TPacket, TResult>(
        Guid requestId,
        Func<TPacket, TResult> selector)
        where TPacket : IVoiceCraftPacket
    {
        while (JsWebRtc.Receive() is { } data)
        {
            lock (_reader)
            {
                _reader.Clear();
                _reader.SetSource(data);
                var found = false;
                TResult? matchedValue = default;
                ProcessPacket(_reader, packet =>
                {
                    if (packet is TPacket typedPacket &&
                        (typedPacket is not IVoiceCraftRIdPacket rIdPacket || rIdPacket.RequestId == requestId))
                    {
                        matchedValue = selector(typedPacket);
                        found = true;
                    }
                    else if (packet is VcDenyResponsePacket denyResponsePacket &&
                             denyResponsePacket.RequestId == requestId)
                    {
                        throw new InvalidOperationException(denyResponsePacket.Reason);
                    }
                    else
                    {
                        ExecutePacket(packet);
                    }
                });

                if (found)
                    return (true, matchedValue);
            }
        }

        return (false, default);
    }

    private static bool TryConsumeCloseReason(out string reason)
    {
        reason = JsWebRtc.ConsumeCloseReason();
        return !string.IsNullOrWhiteSpace(reason);
    }

    private string GetIceServersJson()
    {
        return JsonSerializer.Serialize(settingsService.NetworkSettings.WebRtcIceServers,
            WebRtcVoiceCraftClientJsonContext.Default.ListWebRtcIceServerSettings);
    }

    private static string ToSignalingUrl(string ip, int port)
    {
        if (ip.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
            ip.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            return ip;

        return $"ws://{ip}:{port}/";
    }
}

internal static partial class JsWebRtc
{
    [JSImport("connectAsync", "webrtc.js")]
    public static partial Task ConnectAsync(string signalingUrl, string iceServersJson);

    [JSImport("send", "webrtc.js")]
    public static partial void Send(byte[] data);

    [JSImport("receive", "webrtc.js")]
    public static partial byte[]? Receive();

    [JSImport("consumeCloseReason", "webrtc.js")]
    public static partial string ConsumeCloseReason();

    [JSImport("close", "webrtc.js")]
    public static partial void Close();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<WebRtcIceServerSettings>))]
internal partial class WebRtcVoiceCraftClientJsonContext : JsonSerializerContext;
