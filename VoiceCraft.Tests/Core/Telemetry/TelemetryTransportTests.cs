using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Telemetry;

namespace VoiceCraft.Tests.Core.Telemetry;

public class TelemetryTransportTests
{
    [Fact]
    public async Task SendTelemetryAsync_PreCanceledToken_PropagatesCancellation()
    {
        var transport = new TelemetryTransport();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendTelemetryAsync(new TelemetryEventRequest(), cancellation.Token));
    }

    [Fact]
    public async Task SendDumpAsync_PreCanceledToken_PropagatesCancellation()
    {
        var transport = new TelemetryTransport();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendDumpAsync(new TelemetryDumpRequest(), cancellation.Token));
    }
}
