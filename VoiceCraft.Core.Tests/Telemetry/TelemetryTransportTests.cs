using VoiceCraft.Core.Telemetry;
using Xunit;

namespace VoiceCraft.Core.Tests.Telemetry;

public class TelemetryTransportTests
{
    [Fact]
    public async Task SendTelemetryAsync_PreCanceledToken_PropagatesCancellation()
    {
        var transport = new TelemetryTransport();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendTelemetryAsync(new TelemetryEventRequest(), cancellation.Token));
    }

    [Fact]
    public async Task SendDumpAsync_PreCanceledToken_PropagatesCancellation()
    {
        var transport = new TelemetryTransport();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendDumpAsync(new TelemetryDumpRequest(), cancellation.Token));
    }
}
