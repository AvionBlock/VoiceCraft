# VoiceCraft Tools

This directory contains developer tools for profiling and validating VoiceCraft outside the normal app/test flow.

## AllocHarness

`AllocHarness` is a small allocation and timing benchmark runner. It measures focused hot paths such as visibility updates, audio effects, transport packet parsing, event fanout, and jitter buffering.

Run commands from the repository root:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- [options] [p1 p2 p3]...
```

Use `Release` builds for meaningful measurements. Debug builds are useful only when checking that the harness still runs.

### List Benchmarks

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --list
```

### Run All Benchmarks

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --all
```

### Run One Benchmark

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark visibility
```

Short form:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- -b visibility
```

### Samples

Each scenario is measured several times. The default is `7` samples per scenario.

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark audio-effect-process --samples 10
```

Short form:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- -b audio-effect-process -s 10
```

### Modes

The mode controls which implementation path is measured:

- `checked-out`: measures the code in the current worktree. This is the default.
- `legacy`: measures the legacy-simulated path when the selected benchmark supports it.
- `both`: runs both paths for supported benchmarks and prints a comparison table.

Examples:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark visibility --mode both
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark tcp-frame-read --mode legacy
```

Short form:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- -b visibility -m both
```

Some benchmarks do not have a legacy-simulated path. For those, `--mode both` runs only the checked-out path, and `--mode legacy` fails with an explanatory error.

### Custom Scenarios

Benchmarks accept zero or more scenario triples after the options:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark event-handler-burst 64 32 250
```

Each triple is `p1 p2 p3`. The meaning of each parameter depends on the benchmark and is shown by `--list` and in the benchmark output. Multiple scenarios can be provided in one run:

```powershell
dotnet run --project .\VoiceCraft.Tools\AllocHarness\AllocHarness.csproj -c Release -- --benchmark jitter-buffer 128 160 100 512 160 50
```

If no scenario is provided, the benchmark uses its built-in default scenarios.

### Available Benchmarks

| Name | Purpose |
| --- | --- |
| `visibility` | Measures allocations and timing for `VisibilitySystem.Update()`. |
| `audio-effects` | Measures audio effect collection reads. |
| `tcp-frame-read` | Measures TCP frame read and payload parsing. |
| `tcp-write-payload` | Measures TCP response frame formatting. |
| `http-packed-packets` | Measures HTTP packed packet decode and encode. |
| `http-auth-path` | Measures HTTP bearer auth, packet decode, validation, and in-memory dispatch. |
| `wss-data-tunnel` | Measures WSS data tunnel command decode, packet slicing, and response encoding. |
| `mcapi-broadcast-fanout` | Measures McApi packet serialization and broadcast fanout across peers. |
| `event-handler-burst` | Measures `EventHandlerSystem` queued task bursts drained through `Update()`. |
| `entity-create-sync` | Measures initial McApi peer sync of effects and entity snapshots. |
| `audio-effect-process` | Measures applying audio effects across entities and audio buffers. |
| `jitter-buffer` | Measures `JitterBuffer` add and drain cycles. |

### Output

The harness prints tables to the console and appends an HTML report at:

```text
VoiceCraft.Tools\AllocHarness\report.html
```

The report includes run metadata, scenarios, allocation stats, timing stats, and comparison data when available.

### Interpreting Results

- Allocation values are total bytes allocated across the measured loop.
- `B/op med` is the median bytes per operation for the scenario.
- Time values are median elapsed milliseconds across samples.
- Prefer comparing runs from the same machine, SDK, configuration, and branch state.
- Run a benchmark more than once if the result is close; background system activity can move small timings.

## Unit And Integration Tests

The tools are not a replacement for the normal test projects. Run tests separately:

```powershell
dotnet test .\VoiceCraft.Core.Tests\VoiceCraft.Core.Tests.csproj
dotnet test .\VoiceCraft.Network.Tests\VoiceCraft.Network.Tests.csproj
dotnet test .\VoiceCraft.Client.Tests\VoiceCraft.Client.Tests.csproj
```

