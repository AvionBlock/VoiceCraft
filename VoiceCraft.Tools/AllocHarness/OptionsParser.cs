internal static class OptionsParser
{
    public static Options Parse(string[] args, IReadOnlyDictionary<string, BenchmarkDefinition> benchmarkDefinitions)
    {
        var samples = 7;
        var mode = MeasurementMode.CheckedOut;
        var benchmarkName = "visibility";
        var listBenchmarks = false;
        var runAllBenchmarks = false;
        var positionalValues = new List<int>();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--samples":
                case "-s":
                    samples = ParsePositiveInt(args, ref i, "samples");
                    break;
                case "--mode":
                case "-m":
                    mode = ParseMode(args, ref i);
                    break;
                case "--benchmark":
                case "-b":
                    i++;
                    if (i >= args.Length)
                        throw new ArgumentException(GetUsage());
                    benchmarkName = args[i];
                    break;
                case "--list":
                    listBenchmarks = true;
                    break;
                case "--all":
                    runAllBenchmarks = true;
                    break;
                default:
                    if (!int.TryParse(args[i], out var value))
                        throw new ArgumentException(GetUsage());

                    positionalValues.Add(value);
                    break;
            }
        }

        if (listBenchmarks)
            return new Options([], samples, mode, benchmarkName, ListBenchmarks: true, RunAllBenchmarks: false);

        if (runAllBenchmarks)
            return new Options([], samples, mode, benchmarkName, ListBenchmarks: false, RunAllBenchmarks: true);

        if (!benchmarkDefinitions.TryGetValue(benchmarkName, out var benchmark))
            throw new ArgumentException($"Unknown benchmark '{benchmarkName}'. Use --list to see available benchmarks.");

        if (samples <= 0)
            throw new ArgumentOutOfRangeException(nameof(samples), "Samples must be greater than zero.");

        if (positionalValues.Count == 0)
            return new Options(benchmark.DefaultScenarios, samples, mode, benchmark.Name, ListBenchmarks: false, RunAllBenchmarks: false);

        if (positionalValues.Count % 3 != 0)
            throw new ArgumentException(GetUsage());

        var scenarios = new List<Scenario>(positionalValues.Count / 3);
        for (var i = 0; i < positionalValues.Count; i += 3)
            scenarios.Add(new Scenario(positionalValues[i], positionalValues[i + 1], positionalValues[i + 2]));

        return new Options(scenarios, samples, mode, benchmark.Name, ListBenchmarks: false, RunAllBenchmarks: false);
    }

    private static int ParsePositiveInt(string[] args, ref int index, string name)
    {
        index++;
        if (index >= args.Length || !int.TryParse(args[index], out var value) || value <= 0)
            throw new ArgumentException(GetUsage(), name);

        return value;
    }

    private static MeasurementMode ParseMode(string[] args, ref int index)
    {
        index++;
        if (index >= args.Length)
            throw new ArgumentException(GetUsage(), "mode");

        return args[index].ToLowerInvariant() switch
        {
            "current" => MeasurementMode.CheckedOut,
            "checked-out" => MeasurementMode.CheckedOut,
            "legacy" => MeasurementMode.Legacy,
            "both" => MeasurementMode.Both,
            _ => throw new ArgumentException(GetUsage(), "mode")
        };
    }

    private static string GetUsage()
    {
        return "Usage: dotnet run --project .\\VoiceCraft.Tools\\AllocHarness\\AllocHarness.csproj -c Release -- " +
               "[--list] [--all] [--benchmark NAME] [--samples N] [--mode checked-out|legacy|both] [p1 p2 p3]...";
    }
}
