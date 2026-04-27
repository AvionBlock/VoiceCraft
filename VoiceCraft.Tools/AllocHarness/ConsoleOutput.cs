using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

internal static class ConsoleOutput
{
    public static void PrintBenchmarkList(IReadOnlyDictionary<string, BenchmarkDefinition> benchmarkDefinitions)
    {
        Console.WriteLine("Available benchmarks:");
        foreach (var benchmark in benchmarkDefinitions.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            Console.WriteLine($"- {benchmark.Name}: {benchmark.Description}");
    }

    public static void PrintHeader(Options options, BenchmarkDefinition benchmark)
    {
        Console.WriteLine("Allocation harness");
        Console.WriteLine($"Git: {RuntimeMetadata.GetGitDescription()}");
        Console.WriteLine($".NET: {RuntimeMetadata.GetDotNetVersion()}");
        Console.WriteLine($"OS: {RuntimeMetadata.GetOsDescription()}");
        Console.WriteLine($"Benchmark: {benchmark.Name}");
        Console.WriteLine($"Description: {benchmark.Description}");
        Console.WriteLine($"Mode: {FormatMode(options.Mode)}");
        Console.WriteLine($"Samples per scenario: {options.Samples}");
        Console.WriteLine($"Measurement: {benchmark.MeasurementDescription}");
        Console.WriteLine();
    }

    public static void PrintScenarioTable(IReadOnlyList<ScenarioResult> results, BenchmarkDefinition benchmark)
    {
        var p1Name = benchmark.ParameterNames[0];
        var p2Name = benchmark.ParameterNames[1];
        var p3Name = benchmark.ParameterNames[2];
        var rows = new List<string[]>
        {
            new[]
            {
                "Mode",
                p1Name,
                p2Name,
                p3Name,
                "Samples",
                "Alloc min",
                "Alloc med",
                "Alloc avg",
                "Alloc max",
                "B/op med",
                "Time med (ms)"
            }
        };

        rows.AddRange(results.Select(result => new[]
        {
            FormatScenarioMode(result.Mode),
            result.Scenario.P1.ToString(CultureInfo.InvariantCulture),
            result.Scenario.P2.ToString(CultureInfo.InvariantCulture),
            result.Scenario.P3.ToString(CultureInfo.InvariantCulture),
            result.SampleResults.Count.ToString(CultureInfo.InvariantCulture),
            FormatWholeNumber(result.AllocationStats.Min),
            FormatWholeNumber(result.AllocationStats.Median),
            FormatWholeNumber(result.AllocationStats.Average),
            FormatWholeNumber(result.AllocationStats.Max),
            FormatWholeNumber(result.AllocationStats.Median / GetOperationCount(result.Scenario, benchmark)),
            result.ElapsedStats.Median.ToString("0.###", CultureInfo.InvariantCulture)
        }));

        PrintTable(rows);
    }

    public static void PrintComparisonTable(IReadOnlyList<ScenarioResult> results, BenchmarkDefinition benchmark)
    {
        var grouped = results
            .GroupBy(x => x.Scenario)
            .Select(group => new
            {
                Scenario = group.Key,
                CheckedOut = group.FirstOrDefault(x => x.Mode == ScenarioMode.CheckedOut),
                Legacy = group.FirstOrDefault(x => x.Mode == ScenarioMode.LegacySimulated)
            })
            .Where(x => x.CheckedOut is not null && x.Legacy is not null)
            .ToArray();

        if (grouped.Length == 0) return;

        var p1Name = benchmark.ParameterNames[0];
        var p2Name = benchmark.ParameterNames[1];
        var p3Name = benchmark.ParameterNames[2];

        Console.WriteLine();
        Console.WriteLine("Legacy-simulated vs checked-out");

        var rows = new List<string[]>
        {
            new[]
            {
                p1Name,
                p2Name,
                p3Name,
                "Alloc ratio (L/C)",
                "Time ratio (L/C)"
            }
        };

        rows.AddRange(grouped.Select(group => new[]
        {
            group.Scenario.P1.ToString(CultureInfo.InvariantCulture),
            group.Scenario.P2.ToString(CultureInfo.InvariantCulture),
            group.Scenario.P3.ToString(CultureInfo.InvariantCulture),
            (group.Legacy!.AllocationStats.Median / group.CheckedOut!.AllocationStats.Median).ToString("0.##", CultureInfo.InvariantCulture) + "x",
            (group.Legacy.ElapsedStats.Median / group.CheckedOut.ElapsedStats.Median).ToString("0.##", CultureInfo.InvariantCulture) + "x"
        }));

        PrintTable(rows);
    }

    public static void PrintFooter(BenchmarkDefinition benchmark)
    {
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("- Alloc values are total bytes allocated across the measured loop.");
        Console.WriteLine("- 'checked-out' means the code that is currently checked out in your worktree.");
        Console.WriteLine(benchmark.SupportsLegacyComparison
            ? "- 'legacy-simulated' is the comparison path for the selected benchmark."
            : "- This benchmark has no legacy-simulated path; '--mode both' runs the checked-out path only.");
        if (benchmark.Name == "visibility")
            Console.WriteLine("- visibility legacy-simulated mirrors world.Entities.OfType(...) per entity and AudioEffects materialization inside visibility checks.");
        else if (benchmark.Name == "audio-effects")
            Console.WriteLine("- audio-effects legacy-simulated reads the AudioEffects getter, while checked-out reads AudioEffectsSnapshot.");
        else if (benchmark.Name == "tcp-frame-read")
            Console.WriteLine("- tcp-frame-read legacy-simulated uses per-frame header/payload arrays; checked-out uses a reused header buffer and pooled payload buffer.");
        else if (benchmark.Name == "tcp-write-payload")
            Console.WriteLine("- tcp-write-payload legacy-simulated builds an intermediate payload array before the frame; checked-out writes directly into a pooled frame buffer.");
        else if (benchmark.Name == "http-packed-packets")
            Console.WriteLine("- http-packed-packets measures Z85 decode, packet copies, Z85 encode, and UTF-8 response bytes.");
        else if (benchmark.Name == "http-auth-path")
            Console.WriteLine("- http-auth-path measures bearer parsing, request-size validation, Z85 decode, packet deserialization, auth check, and lightweight in-memory dispatch.");
        else if (benchmark.Name == "wss-data-tunnel")
            Console.WriteLine("- wss-data-tunnel measures Z85 command decode, packet extraction, outbound packing, and response command encoding.");
        else if (benchmark.Name == "mcapi-broadcast-fanout")
            Console.WriteLine("- mcapi-broadcast-fanout measures the in-memory cost of serializing one McApi packet and fanning it out across many peers.");
        else if (benchmark.Name == "event-handler-burst")
            Console.WriteLine("- event-handler-burst measures actual EventHandlerSystem task enqueue bursts followed by Update() queue draining.");
        else if (benchmark.Name == "entity-create-sync")
            Console.WriteLine("- entity-create-sync measures the initial McApi sync path that sends current effects and entity snapshots to a newly connected peer.");
        else if (benchmark.Name == "audio-effect-process")
            Console.WriteLine("- audio-effect-process measures repeated effect.Process(...) passes across many target entities and a stereo buffer.");
        else if (benchmark.Name == "jitter-buffer")
            Console.WriteLine("- jitter-buffer pre-creates packet payloads and measures buffer add/drain work.");
        Console.WriteLine("- Use '--list' to see available benchmarks.");
    }

    public static void PrintReportLocation(string reportPath)
    {
        Console.WriteLine();
        Console.WriteLine($"HTML report: {reportPath}");
    }

    private static int GetOperationCount(Scenario scenario, BenchmarkDefinition benchmark)
    {
        return benchmark.Name switch
        {
            "visibility" => scenario.P3,
            "audio-effects" => scenario.P2,
            "tcp-frame-read" => scenario.P3,
            "tcp-write-payload" => scenario.P3,
            "http-packed-packets" => scenario.P3,
            "http-auth-path" => scenario.P3,
            "wss-data-tunnel" => scenario.P3,
            "mcapi-broadcast-fanout" => scenario.P3,
            "event-handler-burst" => scenario.P3,
            "entity-create-sync" => scenario.P3,
            "audio-effect-process" => scenario.P3,
            "jitter-buffer" => scenario.P3,
            _ => 1
        };
    }

    private static void PrintTable(IReadOnlyList<string[]> rows)
    {
        var widths = new int[rows[0].Length];
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            Console.WriteLine("| " + string.Join(" | ", row.Select((cell, i) => cell.PadLeft(widths[i]))) + " |");

            if (rowIndex != 0) continue;
            Console.WriteLine("|-" + string.Join("-|-", widths.Select(width => new string('-', width))) + "-|");
        }
    }

    private static string FormatMode(MeasurementMode mode)
    {
        return mode switch
        {
            MeasurementMode.CheckedOut => "checked-out",
            MeasurementMode.Legacy => "legacy-simulated",
            MeasurementMode.Both => "both",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string FormatScenarioMode(ScenarioMode mode)
    {
        return mode switch
        {
            ScenarioMode.CheckedOut => "checked-out",
            ScenarioMode.LegacySimulated => "legacy-sim",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string FormatWholeNumber(double value)
    {
        return Math.Round(value).ToString("N0", CultureInfo.InvariantCulture);
    }

}

internal static class RuntimeMetadata
{
    public static string GetGitDescription()
    {
        try
        {
            var commit = RunGit("rev-parse --short HEAD");
            var branch = RunGit("branch --show-current");
            return string.IsNullOrWhiteSpace(branch)
                ? $"{commit} (detached HEAD)"
                : $"{commit} ({branch})";
        }
        catch
        {
            return "unknown";
        }
    }

    public static string GetDotNetVersion()
    {
        return Environment.Version.ToString();
    }

    public static string GetOsDescription()
    {
        return RuntimeInformation.OSDescription;
    }

    private static string RunGit(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
            throw new InvalidOperationException("Failed to start git process.");

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(process.StandardError.ReadToEnd());

        return output;
    }
}
