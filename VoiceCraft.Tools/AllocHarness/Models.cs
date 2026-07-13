using System.Collections.Immutable;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.World;

internal sealed class FakeNetPeer(Guid userGuid, Guid serverUserGuid, string locale, PositioningType positioningType)
    : VoiceCraftNetPeer(null, userGuid, serverUserGuid, locale, positioningType)
{
    public override VcConnectionState ConnectionState => VcConnectionState.Connected;
}

internal delegate void FakeEffectProcessorAction(
    VoiceCraftEntity from,
    VoiceCraftEntity to,
    ushort effectBitmask,
    Span<float> buffer);

internal sealed class FakeAudioEffectProcessor : IAudioEffectProcessor
{
    private readonly FakeEffectProcessorAction _process;
    private bool _disposed;

    public IAudioEffect Effect { get; }
    public VoiceCraftEntity Entity { get; }
    public event Action<IAudioEffectProcessor>? OnDisposed;

    public FakeAudioEffectProcessor(
        IAudioEffect effect,
        VoiceCraftEntity entity,
        FakeEffectProcessorAction process)
    {
        Effect = effect;
        Entity = entity;
        _process = process;
        Effect.OnDisposed += OnEffectDisposed;
    }

    public void Process(VoiceCraftEntity to, Span<float> buffer)
    {
        _process(Entity, to, Effect.Bitmask, buffer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Effect.OnDisposed -= OnEffectDisposed;
        try
        {
            OnDisposed?.Invoke(this);
        }
        finally
        {
            OnDisposed = null;
            GC.SuppressFinalize(this);
        }
    }

    private void OnEffectDisposed(IAudioEffect _)
    {
        Dispose();
    }
}

internal sealed class FakeVisibleEffect(bool result) : IAudioEffect, IVisible
{
    private bool _disposed;
    private bool _result = result;

    public EffectType EffectType => EffectType.Visibility;
    public ushort Bitmask { get; set; }
    public event Action<IAudioEffect>? OnDisposed;

    public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
        new FakeAudioEffectProcessor(this, entity, Process);

    public void Update(IAudioEffect audioEffect)
    {
        if (audioEffect is not FakeVisibleEffect visibleEffect)
            throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
        Bitmask = visibleEffect.Bitmask;
        _result = visibleEffect._result;
    }

    public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
    {
        return _result;
    }

    public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
    {
    }

    public void Serialize(NetDataWriter writer)
    {
    }

    public void Deserialize(NetDataReader reader)
    {
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            OnDisposed?.Invoke(this);
        }
        finally
        {
            OnDisposed = null;
            GC.SuppressFinalize(this);
        }
    }
}

internal sealed class FakeProcessingEffect(int stride) : IAudioEffect
{
    private bool _disposed;
    private int _stride = Math.Max(1, stride);

    public EffectType EffectType => EffectType.Echo;
    public ushort Bitmask { get; set; }
    public event Action<IAudioEffect>? OnDisposed;

    public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
        new FakeAudioEffectProcessor(this, entity, Process);

    public void Update(IAudioEffect audioEffect)
    {
        if (audioEffect is not FakeProcessingEffect processingEffect)
            throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
        Bitmask = processingEffect.Bitmask;
        _stride = processingEffect._stride;
    }

    public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
    {
        var scale = 0.02f * ((effectBitmask & 0xF) + 1);
        for (var i = 0; i < buffer.Length; i += _stride)
            buffer[i] = (buffer[i] + from.Position.X + to.Position.Y) * scale;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_stride);
    }

    public void Deserialize(NetDataReader reader)
    {
        _stride = Math.Max(1, reader.GetInt());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            OnDisposed?.Invoke(this);
        }
        finally
        {
            OnDisposed = null;
            GC.SuppressFinalize(this);
        }
    }
}

internal sealed class FakeMcApiPeer(string sessionToken, FakeMcApiServer? server = null, McApiConnectionState connectionState = McApiConnectionState.Connected)
    : McApiNetPeer(server)
{
    public override McApiConnectionState ConnectionState { get; set; } = connectionState;
    public override string SessionToken { get; } = sessionToken;
}

internal sealed class FakeMcApiPayloadPacket(int payloadBytes) : IMcApiPacket
{
    private readonly byte[] _payload = Enumerable.Repeat((byte)0x5A, Math.Max(0, payloadBytes)).ToArray();

    public McApiPacketType PacketType => McApiPacketType.EntityAudioRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)_payload.Length);
        writer.Put(_payload);
    }

    public void Deserialize(NetDataReader reader)
    {
        var length = reader.GetUShort();
        var buffer = new byte[length];
        reader.GetBytes(buffer, length);
    }

    public void Return()
    {
    }
}

internal sealed class FakeVcPayloadPacket(int payloadBytes) : IVoiceCraftPacket
{
    private readonly byte[] _payload = Enumerable.Repeat((byte)0x47, Math.Max(0, payloadBytes)).ToArray();

    public VcPacketType PacketType => VcPacketType.AudioRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)_payload.Length);
        writer.Put(_payload);
    }

    public void Deserialize(NetDataReader reader)
    {
        var length = reader.GetUShort();
        var buffer = new byte[length];
        reader.GetBytes(buffer, length);
    }

    public void Return()
    {
    }
}

internal sealed class FakeMcApiServer(VoiceCraftWorld world, VoiceCraft.Network.Systems.AudioEffectSystem audioEffectSystem)
    : McApiServer(world, audioEffectSystem)
{
    private readonly NetDataWriter _writer = new();
    private readonly List<FakeMcApiPeer> _peers = [];

    public long TotalBytesWritten { get; private set; }
    public int TotalPacketsWritten { get; private set; }

    public override string LoginToken => string.Empty;
    public override uint MaxClients => 10_000;
    public override int ConnectedPeers => _peers.Count;
    public override ImmutableList<McApiNetPeer> Peers => _peers.Cast<McApiNetPeer>().ToImmutableList();
    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public void AddPeer(FakeMcApiPeer peer)
    {
        _peers.Add(peer);
    }

    public void RaisePeerConnected(FakeMcApiPeer peer, string token)
    {
        OnPeerConnected?.Invoke(peer, token);
    }

    public void ResetCounters()
    {
        TotalBytesWritten = 0;
        TotalPacketsWritten = 0;
    }

    public override void Start()
    {
    }

    public override void Update()
    {
    }

    public override void Stop()
    {
    }

    public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        _writer.Put(packet);
        TotalBytesWritten += _writer.Length;
        TotalPacketsWritten++;
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        _writer.Put(packet);
        foreach (var peer in _peers)
        {
            if (excludes.Contains(peer)) continue;
            TotalBytesWritten += _writer.Length;
            TotalPacketsWritten++;
        }
    }

    public override void Disconnect(McApiNetPeer netPeer, bool force = false)
    {
        OnPeerDisconnected?.Invoke(netPeer, netPeer.SessionToken);
    }

    protected override void AcceptRequest(VoiceCraft.Network.Packets.McApiPackets.Request.McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
    }

    protected override void RejectRequest(VoiceCraft.Network.Packets.McApiPackets.Request.McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer)
    {
    }
}

internal sealed class FakeLiteNetVoiceCraftServer(VoiceCraftWorld world) : LiteNetVoiceCraftServer(world)
{
    private readonly NetDataWriter _writer = new();

    public long TotalBytesWritten { get; private set; }
    public int TotalPacketsWritten { get; private set; }

    public void ResetCounters()
    {
        TotalBytesWritten = 0;
        TotalPacketsWritten = 0;
    }

    public override void Start()
    {
    }

    public override void Update()
    {
    }

    public override void Stop()
    {
    }

    public override void SendUnconnectedPacket<T>(System.Net.IPEndPoint endPoint, T packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        _writer.Put(packet);
        TotalBytesWritten += _writer.Length;
        TotalPacketsWritten++;
    }

    public override void SendPacket<T>(VoiceCraftNetPeer vcNetPeer, T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        _writer.Put(packet);
        TotalBytesWritten += _writer.Length;
        TotalPacketsWritten++;
    }

    public override void Broadcast<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable, params VoiceCraftNetPeer?[] excludes)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        _writer.Put(packet);
        TotalBytesWritten += _writer.Length;
        TotalPacketsWritten++;
    }

    public override void Disconnect(VoiceCraftNetPeer vcNetPeer, string reason, bool force = false)
    {
    }

    public override void DisconnectAll(string? reason = null)
    {
    }
}

internal sealed record Options(
    IReadOnlyList<Scenario> Scenarios,
    int Samples,
    MeasurementMode Mode,
    string BenchmarkName,
    bool ListBenchmarks,
    bool RunAllBenchmarks);

internal sealed record BenchmarkDefinition(
    string Name,
    string Description,
    string[] ParameterNames,
    IReadOnlyList<Scenario> DefaultScenarios,
    string MeasurementDescription,
    bool SupportsLegacyComparison,
    Func<Scenario, ScenarioMode, SampleResult> CreateSampleResult);

internal readonly record struct Scenario(int P1, int P2, int P3);

internal sealed record ScenarioResult(
    Scenario Scenario,
    ScenarioMode Mode,
    IReadOnlyList<SampleResult> SampleResults,
    NumericStats AllocationStats,
    NumericStats ElapsedStats);

internal sealed record BenchmarkRunResult(
    BenchmarkDefinition Benchmark,
    Options Options,
    IReadOnlyList<ScenarioResult> ScenarioResults);

internal sealed record HarnessRunReport(
    DateTimeOffset StartedAtUtc,
    string GitDescription,
    string DotNetVersion,
    string OsDescription,
    IReadOnlyList<BenchmarkRunResult> Benchmarks);

internal readonly record struct SampleResult(long AllocatedBytes, TimeSpan Elapsed);

internal readonly record struct NumericStats(double Min, double Median, double Average, double Max);

internal enum MeasurementMode
{
    CheckedOut,
    Legacy,
    Both
}

internal enum ScenarioMode
{
    CheckedOut,
    LegacySimulated
}

internal static class ProtocolConstants
{
    public const int TcpFrameHeaderSize = 12;
    public const int MaxTcpFramePayloadLength = 1024 * 1024;
    public const int TcpFrameMagic = 0x4D435450;
    public const ushort TcpFrameVersion = 1;
    public const ushort TcpRequestKind = 1;
    public const ushort TcpResponseKind = 2;
}
