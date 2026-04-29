var benchmarkDefinitions = BenchmarkCatalog.Build();
var options = OptionsParser.Parse(args, benchmarkDefinitions);

if (options.ListBenchmarks)
{
    ConsoleOutput.PrintBenchmarkList(benchmarkDefinitions);
    return;
}

if (options.RunAllBenchmarks)
{
    var orderedBenchmarks = benchmarkDefinitions.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    var runResults = new List<BenchmarkRunResult>(orderedBenchmarks.Length);
    for (var index = 0; index < orderedBenchmarks.Length; index++)
    {
        var selectedBenchmark = orderedBenchmarks[index];
        var selectedOptions = options with
        {
            BenchmarkName = selectedBenchmark.Name,
            Scenarios = selectedBenchmark.DefaultScenarios
        };

        runResults.Add(BenchmarkRunner.Run(selectedBenchmark, selectedOptions));
        if (index < orderedBenchmarks.Length - 1)
            Console.WriteLine();
    }

    var reportPath = HtmlReportWriter.Append(new HarnessRunReport(
        DateTimeOffset.UtcNow,
        RuntimeMetadata.GetGitDescription(),
        RuntimeMetadata.GetDotNetVersion(),
        RuntimeMetadata.GetOsDescription(),
        runResults));
    ConsoleOutput.PrintReportLocation(reportPath);
    return;
}

var benchmarkRun = BenchmarkRunner.Run(benchmarkDefinitions[options.BenchmarkName], options);
var singleReportPath = HtmlReportWriter.Append(new HarnessRunReport(
    DateTimeOffset.UtcNow,
    RuntimeMetadata.GetGitDescription(),
    RuntimeMetadata.GetDotNetVersion(),
    RuntimeMetadata.GetOsDescription(),
    [benchmarkRun]));
ConsoleOutput.PrintReportLocation(singleReportPath);
