internal static class BenchmarkRunner
{
    public static BenchmarkRunResult Run(BenchmarkDefinition benchmark, Options options)
    {
        if (options.Mode == MeasurementMode.Legacy && !benchmark.SupportsLegacyComparison)
            throw new ArgumentException($"Benchmark '{benchmark.Name}' does not have a legacy-simulated comparison path.");

        var results = new List<ScenarioResult>();
        foreach (var scenario in options.Scenarios)
        {
            if (options.Mode is MeasurementMode.CheckedOut or MeasurementMode.Both)
                results.Add(MeasureScenario(benchmark, scenario, ScenarioMode.CheckedOut, options.Samples));

            if (benchmark.SupportsLegacyComparison && options.Mode is (MeasurementMode.Legacy or MeasurementMode.Both))
                results.Add(MeasureScenario(benchmark, scenario, ScenarioMode.LegacySimulated, options.Samples));
        }

        ConsoleOutput.PrintHeader(options, benchmark);
        ConsoleOutput.PrintScenarioTable(results, benchmark);
        ConsoleOutput.PrintComparisonTable(results, benchmark);
        ConsoleOutput.PrintFooter(benchmark);

        return new BenchmarkRunResult(benchmark, options, results);
    }

    private static ScenarioResult MeasureScenario(BenchmarkDefinition benchmark, Scenario scenario, ScenarioMode mode, int samples)
    {
        var sampleResults = new List<SampleResult>(samples);
        for (var sampleIndex = 0; sampleIndex < samples; sampleIndex++)
            sampleResults.Add(benchmark.CreateSampleResult(scenario, mode));

        return new ScenarioResult(
            scenario,
            mode,
            sampleResults,
            Stats.BuildLongStats(sampleResults.Select(x => x.AllocatedBytes).ToArray()),
            Stats.BuildDoubleStats(sampleResults.Select(x => x.Elapsed.TotalMilliseconds).ToArray()));
    }
}
