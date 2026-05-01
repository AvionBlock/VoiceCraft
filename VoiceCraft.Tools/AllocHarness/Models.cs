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
    : VoiceCraftNetPeer(userGuid, serverUserGuid, locale, positioningType)
{
    public override VcConnectionState ConnectionState => VcConnectionState.Connected;
}

internal sealed class FakeVisibleEffect(bool result) : IAudioEffect, IVisible
{
    public EffectType EffectType => EffectType.Visibility;

    public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
    {
        return result;
    }

    public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
    {
    }

    public void Reset()
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
    }
}

internal sealed class FakeProcessingEffect(int stride) : IAudioEffect
{
    public EffectType EffectType => EffectType.Echo;

    public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
    {
        var scale = 0.02f * ((effectBitmask & 0xF) + 1);
        for (var i = 0; i < buffer.Length; i += stride)
            buffer[i] = (buffer[i] + from.CaveFactor + to.MuffleFactor) * scale;
    }

    public void Reset()
    {
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(stride);
    }

    public void Deserialize(NetDataReader reader)
    {
        _ = reader.GetInt();
    }

    public void Dispose()
    {
    }
}

internal sealed class FakeMcApiPeer(string sessionToken, McApiConnectionState connectionState = McApiConnectionState.Connected)
    : McApiNetPeer
{
    public override McApiConnectionState ConnectionState { get; } = connectionState;
    public override string SessionToken { get; } = sessionToken;
}

internal sealed class FakeMcApiPayloadPacket(int payloadBytes) : IMcApiPacket
{
    private readonly byte[] _payload = Enumerable.Repeat((byte)0x5A, Math.Max(0, payloadBytes)).ToArray();

    public McApiPacketType PacketType => McApiPacketType.OnEntityAudioReceived;

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
}

internal sealed class FakeVcPayloadPacket(int payloadBytes) : IVoiceCraftPacket
{
    private readonly byte[] _payload = Enumerable.Repeat((byte)0x47, Math.Max(0, payloadBytes)).ToArray();

    public VcPacketType PacketType => VcPacketType.OnEntityAudioReceived;

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
    public override event Action<McApiNetPeer, string>? OnPeerConnected;
    public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public void AddPeer(FakeMcApiPeer peer)
    {
        peer.Tag = this;
        _peers.Add(peer);
    }

    public void RaisePeerConnected(FakeMcApiPeer peer, string token)
    {
        peer.Tag = this;
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
        try
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            TotalBytesWritten += _writer.Length;
            TotalPacketsWritten++;
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
    {
        try
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
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Disconnect(McApiNetPeer netPeer, bool force = false)
    {
        OnPeerDisconnected?.Invoke(netPeer, netPeer.SessionToken);
    }

    protected override void AcceptRequest(VoiceCraft.Network.Packets.McApiPackets.Request.McApiLoginRequestPacket packet, object? data)
    {
    }

    protected override void RejectRequest(VoiceCraft.Network.Packets.McApiPackets.Request.McApiLoginRequestPacket packet, string reason, object? data)
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
        try
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            TotalBytesWritten += _writer.Length;
            TotalPacketsWritten++;
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void SendPacket<T>(VoiceCraftNetPeer vcNetPeer, T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        try
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            TotalBytesWritten += _writer.Length;
            TotalPacketsWritten++;
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Broadcast<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable, params VoiceCraftNetPeer?[] excludes)
    {
        try
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            TotalBytesWritten += _writer.Length;
            TotalPacketsWritten++;
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
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
