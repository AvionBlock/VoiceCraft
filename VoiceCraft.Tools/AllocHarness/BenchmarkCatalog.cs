internal static class BenchmarkCatalog
{
    public static Dictionary<string, BenchmarkDefinition> Build()
    {
        return new Dictionary<string, BenchmarkDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["visibility"] = new(
                Name: "visibility",
                Description: "Measures allocations and timing for VisibilitySystem.Update().",
                ParameterNames: ["Entities", "Effects", "Updates"],
                DefaultScenarios:
                [
                    new Scenario(40, 1, 20),
                    new Scenario(40, 8, 20),
                    new Scenario(64, 8, 10)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated update calls after one warm-up run.",
                SupportsLegacyComparison: true,
                CreateSampleResult: Measurements.MeasureVisibilitySample),

            ["audio-effects"] = new(
                Name: "audio-effects",
                Description: "Measures allocations and timing for audio effect collection reads.",
                ParameterNames: ["Effects", "Reads", "Unused"],
                DefaultScenarios:
                [
                    new Scenario(1, 10_000, 0),
                    new Scenario(4, 10_000, 0),
                    new Scenario(8, 10_000, 0)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated effect collection reads after one warm-up run.",
                SupportsLegacyComparison: true,
                CreateSampleResult: Measurements.MeasureAudioEffectsSample),

            ["tcp-frame-read"] = new(
                Name: "tcp-frame-read",
                Description: "Measures allocations and timing for TCP frame read and payload parsing.",
                ParameterNames: ["Packets", "PacketBytes", "Frames"],
                DefaultScenarios:
                [
                    new Scenario(4, 64, 1_000),
                    new Scenario(8, 256, 1_000),
                    new Scenario(16, 512, 500)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated TCP frame read/parse operations.",
                SupportsLegacyComparison: true,
                CreateSampleResult: Measurements.MeasureTcpFrameReadSample),

            ["tcp-write-payload"] = new(
                Name: "tcp-write-payload",
                Description: "Measures allocations and timing for TCP response frame formatting.",
                ParameterNames: ["Packets", "PacketBytes", "Frames"],
                DefaultScenarios:
                [
                    new Scenario(4, 64, 1_000),
                    new Scenario(8, 256, 1_000),
                    new Scenario(16, 512, 500)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated TCP response frame writes into memory.",
                SupportsLegacyComparison: true,
                CreateSampleResult: Measurements.MeasureTcpWritePayloadSample),

            ["http-packed-packets"] = new(
                Name: "http-packed-packets",
                Description: "Measures allocations and timing for HTTP packed packet decode and encode.",
                ParameterNames: ["Packets", "PacketBytes", "Requests"],
                DefaultScenarios:
                [
                    new Scenario(4, 64, 1_000),
                    new Scenario(8, 256, 500),
                    new Scenario(16, 512, 250)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated HTTP packed-packet decode/encode operations.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureHttpPackedPacketsSample),

            ["http-auth-path"] = new(
                Name: "http-auth-path",
                Description: "Measures allocations and timing for HTTP bearer auth, packet decode, validation, and in-memory dispatch.",
                ParameterNames: ["Packets", "PacketBytes", "Requests"],
                DefaultScenarios:
                [
                    new Scenario(1, 32, 5_000),
                    new Scenario(4, 64, 2_000),
                    new Scenario(8, 128, 1_000)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated HTTP auth-path request handling without sockets.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureHttpAuthPathSample),

            ["wss-data-tunnel"] = new(
                Name: "wss-data-tunnel",
                Description: "Measures allocations and timing for WSS data tunnel decode, packet slicing, and response encoding.",
                ParameterNames: ["Packets", "PacketBytes", "Commands"],
                DefaultScenarios:
                [
                    new Scenario(4, 64, 1_000),
                    new Scenario(8, 256, 500),
                    new Scenario(16, 512, 250)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated WSS data-tunnel command handling.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureWssDataTunnelSample),

            ["mcapi-broadcast-fanout"] = new(
                Name: "mcapi-broadcast-fanout",
                Description: "Measures allocations and timing for McApi packet serialization and broadcast fanout across peers.",
                ParameterNames: ["Peers", "PacketBytes", "Broadcasts"],
                DefaultScenarios:
                [
                    new Scenario(16, 64, 2_000),
                    new Scenario(64, 256, 1_000),
                    new Scenario(256, 512, 250)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated McApi broadcast calls.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureMcApiBroadcastFanoutSample),

            ["event-handler-burst"] = new(
                Name: "event-handler-burst",
                Description: "Measures allocations and timing for EventHandlerSystem task bursts drained through Update().",
                ParameterNames: ["QueuedTasks", "VisiblePeers", "Bursts"],
                DefaultScenarios:
                [
                    new Scenario(32, 8, 500),
                    new Scenario(64, 32, 250),
                    new Scenario(128, 64, 100)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated event-enqueue bursts and queue draining.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureEventHandlerBurstSample),

            ["entity-create-sync"] = new(
                Name: "entity-create-sync",
                Description: "Measures allocations and timing for initial McApi peer sync of effects and entity snapshots.",
                ParameterNames: ["Entities", "Effects", "Connects"],
                DefaultScenarios:
                [
                    new Scenario(16, 4, 500),
                    new Scenario(64, 8, 250),
                    new Scenario(128, 16, 100)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated simulated peer-connect sync runs.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureEntityCreateSyncSample),

            ["audio-effect-process"] = new(
                Name: "audio-effect-process",
                Description: "Measures allocations and timing for applying audio effects across entities and audio buffers.",
                ParameterNames: ["Effects", "Entities", "Runs"],
                DefaultScenarios:
                [
                    new Scenario(4, 8, 2_000),
                    new Scenario(8, 32, 1_000),
                    new Scenario(16, 64, 250)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated audio effect processing passes.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureAudioEffectProcessSample),

            ["jitter-buffer"] = new(
                Name: "jitter-buffer",
                Description: "Measures allocations and timing for JitterBuffer add and drain cycles.",
                ParameterNames: ["Packets", "PacketBytes", "Runs"],
                DefaultScenarios:
                [
                    new Scenario(128, 160, 100),
                    new Scenario(512, 160, 50),
                    new Scenario(1_024, 320, 25)
                ],
                MeasurementDescription: "Steady-state allocations and elapsed time across repeated JitterBuffer fill/drain runs.",
                SupportsLegacyComparison: false,
                CreateSampleResult: Measurements.MeasureJitterBufferSample)
        };
    }
}
