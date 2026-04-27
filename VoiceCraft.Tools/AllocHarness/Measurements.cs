using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Audio;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;
using VoiceCraft.Server.Systems;

internal static class Measurements
{
    public static SampleResult MeasureVisibilitySample(Scenario scenario, ScenarioMode mode)
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);

        for (var i = 0; i < scenario.P1; i++)
            world.AddEntity(CreateNetworkEntity(i + 1));

        for (var i = 0; i < scenario.P2; i++)
            effectSystem.SetEffect((ushort)(1 << i), new FakeVisibleEffect(true));

        Action action = mode switch
        {
            ScenarioMode.CheckedOut => visibilitySystem.Update,
            ScenarioMode.LegacySimulated => () => _ = RunLegacyVisibilityPass(world, effectSystem),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return MeasureAction(action, scenario.P3);
    }

    public static SampleResult MeasureAudioEffectsSample(Scenario scenario, ScenarioMode mode)
    {
        using var effectSystem = new AudioEffectSystem();
        for (var i = 0; i < scenario.P1; i++)
            effectSystem.SetEffect((ushort)(1 << i), new FakeVisibleEffect(true));

        Action action = mode switch
        {
            ScenarioMode.CheckedOut => () => GC.KeepAlive(effectSystem.AudioEffectsSnapshot),
            ScenarioMode.LegacySimulated => () => GC.KeepAlive(effectSystem.AudioEffects),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return MeasureAction(action, scenario.P2);
    }

    public static SampleResult MeasureTcpFrameReadSample(Scenario scenario, ScenarioMode mode)
    {
        var packets = BuildPackets(scenario.P1, scenario.P2);
        var payload = BuildTcpPayload(string.Empty, packets);
        var frame = BuildTcpFrame(payload, ProtocolConstants.TcpRequestKind);
        var headerBuffer = ArrayPool<byte>.Shared.Rent(ProtocolConstants.TcpFrameHeaderSize);

        try
        {
            Action action = mode switch
            {
                ScenarioMode.CheckedOut => () => RunTcpFrameReadCheckedOut(frame, headerBuffer),
                ScenarioMode.LegacySimulated => () => RunTcpFrameReadLegacy(frame),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            return MeasureAction(action, scenario.P3);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    public static SampleResult MeasureTcpWritePayloadSample(Scenario scenario, ScenarioMode mode)
    {
        var packets = BuildPackets(scenario.P1, scenario.P2);

        Action action = mode switch
        {
            ScenarioMode.CheckedOut => () => RunTcpWritePayloadCheckedOut(packets),
            ScenarioMode.LegacySimulated => () => RunTcpWritePayloadLegacy(packets),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return MeasureAction(action, scenario.P3);
    }

    public static SampleResult MeasureHttpPackedPacketsSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "HTTP packed packets has no legacy comparison path.");

        var packets = BuildPackets(scenario.P1, scenario.P2);
        var encodedRequest = BuildHttpPackedString(packets);
        var reader = new NetDataReader();
        var writer = new NetDataWriter();

        return MeasureAction(() => RunHttpPackedPacketsCheckedOut(encodedRequest, reader, writer), scenario.P3);
    }

    public static SampleResult MeasureHttpAuthPathSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "HTTP auth path has no legacy comparison path.");

        var requestBody = BuildHttpAuthRequestBody(scenario.P1, scenario.P2);
        var authorizationHeader = "Bearer session-token";
        var reader = new NetDataReader();
        var packet = new McApiSetEntityPositionRequestPacket();
        var peer = new FakeMcApiPeer("session-token");
        var entity = new VoiceCraftEntity(100);

        return MeasureAction(() => RunHttpAuthPathCheckedOut(authorizationHeader, requestBody, reader, packet, peer, entity), scenario.P3);
    }

    public static SampleResult MeasureWssDataTunnelSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "WSS data tunnel has no legacy comparison path.");

        var packets = BuildPackets(scenario.P1, scenario.P2);
        var encodedCommand = BuildHttpPackedString(packets);
        var reader = new NetDataReader();
        var writer = new NetDataWriter();

        return MeasureAction(() => RunWssDataTunnelCheckedOut(encodedCommand, packets, reader, writer), scenario.P3);
    }

    public static SampleResult MeasureMcApiBroadcastFanoutSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "McApi broadcast fanout has no legacy comparison path.");

        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var server = new FakeMcApiServer(world, effectSystem);
        for (var i = 0; i < scenario.P1; i++)
            server.AddPeer(new FakeMcApiPeer($"peer-{i}"));

        return MeasureAction(() =>
        {
            server.ResetCounters();
            server.Broadcast(new FakeMcApiPayloadPacket(scenario.P2));
            GC.KeepAlive(server.TotalBytesWritten);
        }, scenario.P3);
    }

    public static SampleResult MeasureEventHandlerBurstSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Event handler burst has no legacy comparison path.");

        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        using var liteNetServer = new FakeLiteNetVoiceCraftServer(world);
        using var mcApiServer = new FakeMcApiServer(world, effectSystem);
        using var eventHandlerSystem = new EventHandlerSystem(liteNetServer, [mcApiServer], effectSystem, world);

        var sourceEntity = new VoiceCraftEntity(1);
        world.AddEntity(sourceEntity);

        for (var i = 0; i < scenario.P2; i++)
        {
            var visible = CreateNetworkEntity(i + 1000);
            world.AddEntity(visible);
            sourceEntity.AddVisibleEntity(visible);
        }

        eventHandlerSystem.Update();

        var positionSeed = 0f;
        return MeasureAction(() =>
        {
            liteNetServer.ResetCounters();
            mcApiServer.ResetCounters();

            for (var i = 0; i < scenario.P1; i++)
            {
                positionSeed += 1f;
                sourceEntity.Position = new Vector3(positionSeed, i, 0f);
            }

            eventHandlerSystem.Update();
            GC.KeepAlive(liteNetServer.TotalBytesWritten + mcApiServer.TotalBytesWritten);
        }, scenario.P3);
    }

    public static SampleResult MeasureEntityCreateSyncSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Entity create sync has no legacy comparison path.");

        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        using var liteNetServer = new FakeLiteNetVoiceCraftServer(world);
        using var mcApiServer = new FakeMcApiServer(world, effectSystem);
        using var eventHandlerSystem = new EventHandlerSystem(liteNetServer, [mcApiServer], effectSystem, world);

        for (var i = 0; i < scenario.P1; i++)
            world.AddEntity(CreateEntityForSync(i));

        for (var i = 0; i < scenario.P2; i++)
            effectSystem.SetEffect((ushort)(1 << i), new FakeVisibleEffect(true));

        eventHandlerSystem.Update();

        var peer = new FakeMcApiPeer("entity-create-sync");
        return MeasureAction(() =>
        {
            liteNetServer.ResetCounters();
            mcApiServer.ResetCounters();
            mcApiServer.RaisePeerConnected(peer, peer.SessionToken);
            eventHandlerSystem.Update();
            GC.KeepAlive(mcApiServer.TotalBytesWritten + liteNetServer.TotalBytesWritten);
        }, scenario.P3);
    }

    public static SampleResult MeasureAudioEffectProcessSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Audio effect process has no legacy comparison path.");

        using var effectSystem = new AudioEffectSystem();
        var source = new VoiceCraftEntity(1)
        {
            CaveFactor = 0.4f,
            MuffleFactor = 0.1f
        };
        var targets = Enumerable.Range(0, scenario.P2)
            .Select(i => new VoiceCraftEntity(i + 2)
            {
                CaveFactor = (i % 5) / 5f,
                MuffleFactor = (i % 7) / 7f
            })
            .ToArray();

        for (var i = 0; i < scenario.P1; i++)
            effectSystem.SetEffect((ushort)(1 << i), new FakeProcessingEffect((i % 4) + 1));

        var buffer = new float[960];

        return MeasureAction(() =>
        {
            Array.Fill(buffer, 0.25f);
            foreach (var target in targets)
                foreach (var effect in effectSystem.AudioEffectsSnapshot)
                    effect.Value.Process(source, target, effect.Key, buffer);

            GC.KeepAlive(buffer[0]);
        }, scenario.P3);
    }

    public static SampleResult MeasureJitterBufferSample(Scenario scenario, ScenarioMode mode)
    {
        if (mode != ScenarioMode.CheckedOut)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Jitter buffer has no legacy comparison path.");

        var jitterBuffer = new JitterBuffer(TimeSpan.Zero);
        var packets = BuildJitterPackets(scenario.P1, scenario.P2);

        return MeasureAction(() => RunJitterBufferCycle(jitterBuffer, packets), scenario.P3);
    }

    private static SampleResult MeasureAction(Action action, int iterations)
    {
        action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetTotalAllocatedBytes(true);
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
            action();
        stopwatch.Stop();
        var after = GC.GetTotalAllocatedBytes(true);

        return new SampleResult(after - before, stopwatch.Elapsed);
    }

    private static int RunLegacyVisibilityPass(VoiceCraftWorld world, AudioEffectSystem effectSystem)
    {
        var visibleCount = 0;
        foreach (var entity in world.Entities)
        {
            entity.TrimDeadEntities();

            var visibleNetworkEntities = world.Entities.OfType<VoiceCraftNetworkEntity>();
            foreach (var possibleEntity in visibleNetworkEntities)
            {
                if (possibleEntity.Id == entity.Id) continue;
                if ((entity.TalkBitmask & possibleEntity.ListenBitmask) == 0)
                {
                    entity.RemoveVisibleEntity(possibleEntity);
                    continue;
                }

                var visible = true;
                foreach (var effect in effectSystem.AudioEffects)
                {
                    if (effect.Value is not IVisible visibleEffect) continue;
                    if (visibleEffect.Visibility(entity, possibleEntity, effect.Key)) continue;

                    visible = false;
                    break;
                }

                if (!visible)
                {
                    entity.RemoveVisibleEntity(possibleEntity);
                    continue;
                }

                entity.AddVisibleEntity(possibleEntity);
                visibleCount++;
            }
        }

        return visibleCount;
    }

    private static byte[][] BuildPackets(int packetCount, int packetBytes)
    {
        var packets = new byte[packetCount][];
        for (var packetIndex = 0; packetIndex < packets.Length; packetIndex++)
        {
            var packet = new byte[packetBytes];
            for (var byteIndex = 0; byteIndex < packet.Length; byteIndex++)
                packet[byteIndex] = (byte)((packetIndex + byteIndex) & 0xFF);
            packets[packetIndex] = packet;
        }

        return packets;
    }

    private static JitterPacket[] BuildJitterPackets(int packetCount, int packetBytes)
    {
        var packets = new JitterPacket[packetCount];
        for (var i = 0; i < packets.Length; i++)
            packets[i] = new JitterPacket((ushort)i, BuildPacketPayload(i, packetBytes));
        return packets;
    }

    private static byte[] BuildPacketPayload(int seed, int packetBytes)
    {
        var data = new byte[packetBytes];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)((seed + i) & 0xFF);
        return data;
    }

    private static byte[] BuildTcpPayload(string token, IReadOnlyList<byte[]> packets)
    {
        var tokenLength = string.IsNullOrEmpty(token) ? 0 : Encoding.UTF8.GetByteCount(token);
        var payloadLength = 4 + tokenLength + 4;
        foreach (var packet in packets)
            payloadLength += 4 + packet.Length;

        var payload = new byte[payloadLength];
        var offset = 0;
        WriteInt32(payload, ref offset, tokenLength);
        if (tokenLength > 0)
            offset += Encoding.UTF8.GetBytes(token, payload.AsSpan(offset, tokenLength));

        WriteInt32(payload, ref offset, packets.Count);
        foreach (var packet in packets)
        {
            WriteInt32(payload, ref offset, packet.Length);
            packet.CopyTo(payload.AsSpan(offset));
            offset += packet.Length;
        }

        return payload;
    }

    private static byte[] BuildTcpFrame(ReadOnlySpan<byte> payload, ushort kind)
    {
        var frame = new byte[ProtocolConstants.TcpFrameHeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), ProtocolConstants.TcpFrameMagic);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4, 2), ProtocolConstants.TcpFrameVersion);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(6, 2), kind);
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(8, 4), payload.Length);
        payload.CopyTo(frame.AsSpan(ProtocolConstants.TcpFrameHeaderSize));
        return frame;
    }

    private static void RunTcpFrameReadCheckedOut(ReadOnlySpan<byte> frame, byte[] headerBuffer)
    {
        frame[..ProtocolConstants.TcpFrameHeaderSize].CopyTo(headerBuffer);
        if (!TryReadTcpFrameHeader(headerBuffer, ProtocolConstants.TcpRequestKind, out var payloadLength))
            throw new InvalidOperationException("Invalid TCP frame header.");

        var payloadBuffer = ArrayPool<byte>.Shared.Rent(payloadLength);
        try
        {
            frame.Slice(ProtocolConstants.TcpFrameHeaderSize, payloadLength).CopyTo(payloadBuffer);
            if (!TryReadTcpPayload(payloadBuffer.AsSpan(0, payloadLength), out var token, out var packets))
                throw new InvalidOperationException("Invalid TCP payload.");

            GC.KeepAlive(token);
            GC.KeepAlive(packets);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payloadBuffer);
        }
    }

    private static void RunTcpFrameReadLegacy(ReadOnlySpan<byte> frame)
    {
        var headerBuffer = new byte[ProtocolConstants.TcpFrameHeaderSize];
        frame[..ProtocolConstants.TcpFrameHeaderSize].CopyTo(headerBuffer);
        if (!TryReadTcpFrameHeader(headerBuffer, ProtocolConstants.TcpRequestKind, out var payloadLength))
            throw new InvalidOperationException("Invalid TCP frame header.");

        var payloadBuffer = new byte[payloadLength];
        frame.Slice(ProtocolConstants.TcpFrameHeaderSize, payloadLength).CopyTo(payloadBuffer);
        if (!TryReadTcpPayload(payloadBuffer, out var token, out var packets))
            throw new InvalidOperationException("Invalid TCP payload.");

        GC.KeepAlive(token);
        GC.KeepAlive(packets);
    }

    private static void RunTcpWritePayloadCheckedOut(IReadOnlyList<byte[]> packets)
    {
        var payloadLength = CalculateTcpPayloadLength(tokenLength: 0, packets);
        var frameLength = ProtocolConstants.TcpFrameHeaderSize + payloadLength;
        var frameBuffer = ArrayPool<byte>.Shared.Rent(frameLength);

        try
        {
            WriteTcpFrameHeader(frameBuffer, ProtocolConstants.TcpResponseKind, payloadLength);

            var offset = ProtocolConstants.TcpFrameHeaderSize;
            WriteInt32(frameBuffer, ref offset, 0);
            WriteInt32(frameBuffer, ref offset, packets.Count);
            foreach (var packet in packets)
            {
                WriteInt32(frameBuffer, ref offset, packet.Length);
                packet.CopyTo(frameBuffer.AsSpan(offset));
                offset += packet.Length;
            }

            GC.KeepAlive(frameBuffer[0]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(frameBuffer);
        }
    }

    private static void RunTcpWritePayloadLegacy(IReadOnlyList<byte[]> packets)
    {
        var payloadLength = CalculateTcpPayloadLength(tokenLength: 0, packets);
        var payload = new byte[payloadLength];
        var payloadOffset = 0;
        WriteInt32(payload, ref payloadOffset, 0);
        WriteInt32(payload, ref payloadOffset, packets.Count);
        foreach (var packet in packets)
        {
            WriteInt32(payload, ref payloadOffset, packet.Length);
            packet.CopyTo(payload.AsSpan(payloadOffset));
            payloadOffset += packet.Length;
        }

        var frameBuffer = new byte[ProtocolConstants.TcpFrameHeaderSize + payload.Length];
        WriteTcpFrameHeader(frameBuffer, ProtocolConstants.TcpResponseKind, payload.Length);
        payload.CopyTo(frameBuffer.AsSpan(ProtocolConstants.TcpFrameHeaderSize));
        GC.KeepAlive(frameBuffer);
    }

    private static string BuildHttpPackedString(IReadOnlyList<byte[]> packets)
    {
        var writer = new NetDataWriter();
        foreach (var packet in packets)
        {
            writer.Put((ushort)packet.Length);
            writer.Put(packet);
        }

        return Z85.GetStringWithPadding(writer.AsReadOnlySpan());
    }

    private static void RunHttpPackedPacketsCheckedOut(string encodedRequest, NetDataReader reader, NetDataWriter writer)
    {
        var packets = new List<byte[]>();
        if (!TryReadHttpPackedPackets(encodedRequest, packets, reader))
            throw new InvalidOperationException("Invalid HTTP packed packets.");

        writer.Reset();
        foreach (var packet in packets)
        {
            writer.Put((ushort)packet.Length);
            writer.Put(packet);
        }

        var encodedResponse = Z85.GetStringWithPadding(writer.AsReadOnlySpan());
        var responseBuffer = Encoding.UTF8.GetBytes(encodedResponse);
        GC.KeepAlive(responseBuffer);
    }

    private static string BuildHttpAuthRequestBody(int packetCount, int packetBytes)
    {
        var packets = new byte[packetCount][];
        for (var i = 0; i < packetCount; i++)
        {
            var writer = new NetDataWriter();
            writer.Put((byte)McApiPacketType.SetEntityPositionRequest);
            var packet = new McApiSetEntityPositionRequestPacket()
                .Set(i + 1, new Vector3(i, i + 1, i + 2));
            writer.Put(packet);

            if (packetBytes > writer.Length)
                writer.Put(new byte[packetBytes - writer.Length]);

            packets[i] = writer.CopyData();
        }

        return BuildHttpPackedString(packets);
    }

    private static void RunHttpAuthPathCheckedOut(
        string authorizationHeader,
        string encodedRequest,
        NetDataReader reader,
        McApiSetEntityPositionRequestPacket packet,
        FakeMcApiPeer peer,
        VoiceCraftEntity entity)
    {
        if (!TryGetBearerToken(authorizationHeader, out var token))
            throw new InvalidOperationException("Missing bearer token.");

        var contentLength = Encoding.UTF8.GetByteCount(encodedRequest);
        if (contentLength is <= 0 or > 1_000_000)
            throw new InvalidOperationException("Invalid request length.");

        var packets = new List<byte[]>();
        if (!TryReadHttpPackedPackets(encodedRequest, packets, reader))
            throw new InvalidOperationException("Invalid packed request.");

        foreach (var packetBytes in packets)
        {
            reader.Clear();
            reader.SetSource(packetBytes);
            var packetType = (McApiPacketType)reader.GetByte();
            if (packetType != McApiPacketType.SetEntityPositionRequest)
                continue;

            packet.Deserialize(reader);
            if (!AuthorizePacket(packet, peer, token))
                continue;

            entity.Position = packet.Value;
        }
    }

    private static void RunWssDataTunnelCheckedOut(string encodedCommand, IReadOnlyList<byte[]> outboundPackets, NetDataReader reader, NetDataWriter writer)
    {
        var packets = new List<byte[]>();
        if (!TryReadHttpPackedPackets(encodedCommand, packets, reader))
            throw new InvalidOperationException("Invalid WSS command payload.");

        writer.Reset();
        foreach (var packet in outboundPackets)
        {
            writer.Put((ushort)packet.Length);
            writer.Put(packet);
        }

        var encodedResponse = Z85.GetStringWithPadding(writer.AsReadOnlySpan());
        GC.KeepAlive(packets);
        GC.KeepAlive(encodedResponse);
    }

    private static void RunJitterBufferCycle(JitterBuffer jitterBuffer, IReadOnlyList<JitterPacket> packets)
    {
        jitterBuffer.Reset();
        foreach (var packet in packets)
            jitterBuffer.Add(packet);

        while (jitterBuffer.Get(out var packet))
            GC.KeepAlive(packet);
    }

    private static bool TryReadHttpPackedPackets(string encodedRequest, List<byte[]> packets, NetDataReader reader)
    {
        try
        {
            var packedPackets = Z85.GetBytesWithPadding(encodedRequest);
            reader.Clear();
            reader.SetSource(packedPackets);
            while (!reader.EndOfData)
            {
                var packetSize = reader.GetUShort();
                if (packetSize <= 0) continue;
                if (reader.AvailableBytes < packetSize)
                    return false;

                var packet = new byte[packetSize];
                reader.GetBytes(packet, packetSize);
                packets.Add(packet);
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool AuthorizePacket(IMcApiPacket packet, FakeMcApiPeer netPeer, string token)
    {
        return packet switch
        {
            McApiLoginRequestPacket => true,
            McApiLogoutRequestPacket => true,
            _ => netPeer.ConnectionState == McApiConnectionState.Connected && token == netPeer.SessionToken
        };
    }

    private static bool TryGetBearerToken(string? authorizationHeader, out string token)
    {
        const string bearerPrefix = "Bearer ";

        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        token = authorizationHeader[bearerPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool TryReadTcpFrameHeader(ReadOnlySpan<byte> header, ushort expectedKind, out int payloadLength)
    {
        payloadLength = 0;
        if (header.Length < ProtocolConstants.TcpFrameHeaderSize)
            return false;

        var magic = BinaryPrimitives.ReadInt32BigEndian(header[..4]);
        var version = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(4, 2));
        var kind = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(6, 2));
        payloadLength = BinaryPrimitives.ReadInt32BigEndian(header.Slice(8, 4));

        return magic == ProtocolConstants.TcpFrameMagic &&
               version == ProtocolConstants.TcpFrameVersion &&
               kind == expectedKind &&
               payloadLength is >= 0 and <= ProtocolConstants.MaxTcpFramePayloadLength;
    }

    private static bool TryReadTcpPayload(ReadOnlySpan<byte> payload, out string token, out List<byte[]> packets)
    {
        token = string.Empty;
        packets = [];
        if (payload.Length < 8)
            return false;

        var offset = 0;
        if (!TryReadInt32(payload, ref offset, out var tokenLength) || tokenLength < 0 ||
            payload.Length - offset < tokenLength)
            return false;

        token = tokenLength == 0 ? string.Empty : Encoding.UTF8.GetString(payload.Slice(offset, tokenLength));
        offset += tokenLength;

        if (!TryReadInt32(payload, ref offset, out var packetCount) || packetCount < 0)
            return false;
        if (packetCount > (payload.Length - offset) / 5)
            return false;

        packets = new List<byte[]>(packetCount);
        for (var i = 0; i < packetCount; i++)
        {
            if (!TryReadInt32(payload, ref offset, out var packetLength) || packetLength <= 0 ||
                payload.Length - offset < packetLength)
                return false;

            var packet = new byte[packetLength];
            payload.Slice(offset, packetLength).CopyTo(packet);
            packets.Add(packet);
            offset += packetLength;
        }

        return offset == payload.Length;
    }

    private static bool TryReadInt32(ReadOnlySpan<byte> payload, ref int offset, out int value)
    {
        value = 0;
        if (payload.Length - offset < 4)
            return false;

        value = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset, 4));
        offset += 4;
        return true;
    }

    private static int CalculateTcpPayloadLength(int tokenLength, IReadOnlyList<byte[]> packets)
    {
        var payloadLength = 4 + tokenLength + 4;
        foreach (var packet in packets)
            payloadLength += 4 + packet.Length;
        return payloadLength;
    }

    private static void WriteTcpFrameHeader(byte[] frameBuffer, ushort kind, int payloadLength)
    {
        BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(0, 4), ProtocolConstants.TcpFrameMagic);
        BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(4, 2), ProtocolConstants.TcpFrameVersion);
        BinaryPrimitives.WriteUInt16BigEndian(frameBuffer.AsSpan(6, 2), kind);
        BinaryPrimitives.WriteInt32BigEndian(frameBuffer.AsSpan(8, 4), payloadLength);
    }

    private static void WriteInt32(byte[] payload, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(offset, 4), value);
        offset += 4;
    }

    private static VoiceCraftNetworkEntity CreateNetworkEntity(int id)
    {
        return new VoiceCraftNetworkEntity(
            new FakeNetPeer(Guid.NewGuid(), Guid.NewGuid(), "en-US", PositioningType.Client),
            id);
    }

    private static VoiceCraftEntity CreateEntityForSync(int id)
    {
        return id % 3 == 0
            ? CreateNetworkEntity(id + 1)
            : new VoiceCraftEntity(id + 1)
            {
                Name = $"Entity {id + 1}",
                WorldId = $"world-{id % 4}",
                Position = new Vector3(id, id * 0.5f, id * 0.25f),
                Rotation = new Vector2(id % 360, (id * 2) % 360),
                CaveFactor = (id % 5) / 5f,
                MuffleFactor = (id % 7) / 7f
            };
    }
}
