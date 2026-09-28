using System;
using System.Linq;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Tests.Client.Services;

public class LogServiceTests
{
    [Fact]
    public void Log_TrimsExceptionLogsToLimit()
    {
        LogService.ClearExceptionLogs();

        for (var i = 0; i < 55; i++)
            LogService.Log(new InvalidOperationException($"test-{i}"));

        Assert.InRange(LogService.ExceptionLogs.Count(), 1, 50);
    }
}
