using System.Globalization;
using System.Net;
using System.Text;

internal static class HtmlReportWriter
{
    private const string ReportFileName = "report.html";
    private const string EntriesMarker = "<!-- REPORT_ENTRIES -->";
    private const string TemplateVersionMarker = "data-report-version=\"4\"";

    public static string Append(HarnessRunReport report)
    {
        var reportPath = GetReportPath();
        var document = LoadOrCreateDocument(reportPath);
        var entry = BuildReportEntry(report);
        var updated = document.Replace(EntriesMarker, entry + Environment.NewLine + EntriesMarker, StringComparison.Ordinal);
        File.WriteAllText(reportPath, updated, Encoding.UTF8);
        return reportPath;
    }

    private static string GetReportPath()
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        return Path.Combine(projectDirectory, ReportFileName);
    }

    private static string LoadOrCreateDocument(string reportPath)
    {
        if (!File.Exists(reportPath))
            return BuildDocumentSkeleton();

        var document = File.ReadAllText(reportPath, Encoding.UTF8);
        return document.Contains(TemplateVersionMarker, StringComparison.Ordinal)
            ? document
            : BuildDocumentSkeleton();
    }

    private static string BuildDocumentSkeleton()
    {
        return """
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>AllocHarness Report</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-LN+7fdVzj6u52u30Kp6M/trliBMCMKTyK833zpbD+pXdCLuTusPj697FH4R/5mcr" crossorigin="anonymous">
    <style>
        body { background: #09111f; color: #e7eef8; }
        .page-wrap { max-width: 1560px; }
        .brand { color: #f8fbff; }
        .brand small { color: #95a9c2; }
        .surface, .run-card, .benchmark-card, .metric-card { background: #101a2d; border: 1px solid #22314d; box-shadow: 0 12px 32px rgba(0, 0, 0, 0.22); }
        .toolbar { position: sticky; top: 0; z-index: 10; backdrop-filter: blur(12px); background: rgba(9, 17, 31, 0.82); }
        .table { --bs-table-bg: transparent; --bs-table-color: #e7eef8; --bs-table-border-color: #263650; }
        .table thead th { color: #9eb4cf; font-weight: 600; font-size: 0.82rem; text-transform: uppercase; letter-spacing: 0.03em; white-space: nowrap; }
        .subtle { color: #9eb4cf; }
        .pill { display: inline-block; padding: 0.2rem 0.55rem; border-radius: 999px; font-size: 0.8rem; background: #18243a; color: #d7e6f6; border: 1px solid #2b4060; }
        .ratio-good { color: #78e08f; }
        .ratio-warn { color: #ffb86b; }
        .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 0.85rem; }
        .summary-card { background: #121f34; border: 1px solid #253756; border-radius: 0.9rem; padding: 0.95rem 1rem; }
        .summary-card .label { color: #8ea4bf; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.04em; }
        .summary-card .value { color: #f8fbff; font-size: 1.35rem; font-weight: 700; }
        .empty-state { display: none; }
        .compare-panel { display: none; }
        .compare-results { display: grid; gap: 1rem; }
        .compare-table { display: grid; gap: 0; }
        .compare-grid-head, .compare-run-row { display: grid; grid-template-columns: 72px minmax(260px, 1.4fr) minmax(280px, 1.5fr) repeat(6, minmax(110px, 0.85fr)); gap: 0.75rem; align-items: center; }
        .compare-grid-head { color: #9eb4cf; font-size: 0.77rem; text-transform: uppercase; letter-spacing: 0.04em; padding-bottom: 0.75rem; }
        .compare-row-group { border-top: 1px solid #263650; padding: 0.85rem 0; }
        .compare-run-row { padding: 0.2rem 0; }
        .compare-scenario-meta { color: #cfe0f2; }
        .compare-run-meta { color: #d7e6f6; }
        .compare-divider { margin-top: 0.55rem; border-top: 1px dashed #314665; }
        .compare-run-chip { display: inline-block; padding: 0.18rem 0.5rem; border-radius: 999px; background: #16233a; border: 1px solid #2b4060; font-size: 0.74rem; color: #d7e6f6; margin-right: 0.35rem; }
        .delta-better { color: #78e08f; font-weight: 700; }
        .delta-worse { color: #ff7b72; font-weight: 700; }
        .delta-flat { color: #c8d5e6; }
        .run-card[hidden], .benchmark-card[hidden] { display: none !important; }
        code { color: #a5d8ff; }
        @media (max-width: 1320px) {
            .compare-grid-head, .compare-run-row { grid-template-columns: 1fr; }
        }
    </style>
</head>
<body data-report-version="4">
    <main class="page-wrap container py-4 py-lg-5">
        <section class="mb-4">
            <h1 class="brand h3 mb-1">AllocHarness Report</h1>
            <small>Append-only run log for allocation and timing measurements, with filters, history, and run-vs-run comparisons.</small>
        </section>

        <section class="surface toolbar rounded-4 p-3 p-lg-4 mb-4">
            <div class="row g-3 align-items-end">
                <div class="col-12 col-md-6 col-xl-3">
                    <label class="form-label subtle mb-1" for="filter-search">Search</label>
                    <input id="filter-search" class="form-control bg-dark text-light border-secondary" type="search" placeholder="benchmark, commit, os, dotnet">
                </div>
                <div class="col-6 col-md-3 col-xl-2">
                    <label class="form-label subtle mb-1" for="filter-benchmark">Benchmark</label>
                    <select id="filter-benchmark" class="form-select bg-dark text-light border-secondary">
                        <option value="">All benchmarks</option>
                    </select>
                </div>
                <div class="col-6 col-md-3 col-xl-2">
                    <label class="form-label subtle mb-1" for="filter-mode">Mode</label>
                    <select id="filter-mode" class="form-select bg-dark text-light border-secondary">
                        <option value="">All modes</option>
                    </select>
                </div>
                <div class="col-12 col-md-6 col-xl-3">
                    <label class="form-label subtle mb-1" for="filter-commit">Commit</label>
                    <select id="filter-commit" class="form-select bg-dark text-light border-secondary">
                        <option value="">All commits</option>
                    </select>
                </div>
                <div class="col-12 col-md-6 col-xl-2 d-flex gap-2">
                    <div class="form-check mt-4">
                        <input class="form-check-input" type="checkbox" value="" id="filter-latest">
                        <label class="form-check-label subtle" for="filter-latest">Latest run only</label>
                    </div>
                    <button id="filter-reset" class="btn btn-outline-light ms-auto mt-3 mt-md-4" type="button">Reset</button>
                </div>
            </div>
            <div class="summary-grid mt-4">
                <div class="summary-card">
                    <div class="label">Visible Runs</div>
                    <div id="summary-runs" class="value">0</div>
                </div>
                <div class="summary-card">
                    <div class="label">Visible Benchmarks</div>
                    <div id="summary-benchmarks" class="value">0</div>
                </div>
                <div class="summary-card">
                    <div class="label">Unique Commits</div>
                    <div id="summary-commits" class="value">0</div>
                </div>
                <div class="summary-card">
                    <div class="label">Latest Visible Run</div>
                    <div id="summary-latest" class="value fs-6">-</div>
                </div>
            </div>
        </section>

        <section class="surface compare-panel rounded-4 p-3 p-lg-4 mb-4" id="compare-panel">
            <div class="d-flex flex-column flex-lg-row justify-content-between gap-2 mb-3">
                <div>
                    <h2 class="h5 mb-1">VS Mode</h2>
                    <div class="subtle">Choose two runs and optionally a benchmark. Below, each shared benchmark renders as its own comparison block.</div>
                </div>
            </div>
            <div class="row g-3 align-items-end">
                <div class="col-12 col-md-4">
                    <label class="form-label subtle mb-1" for="compare-left-run">Run A</label>
                    <select id="compare-left-run" class="form-select bg-dark text-light border-secondary"></select>
                </div>
                <div class="col-12 col-md-4">
                    <label class="form-label subtle mb-1" for="compare-right-run">Run B</label>
                    <select id="compare-right-run" class="form-select bg-dark text-light border-secondary"></select>
                </div>
                <div class="col-12 col-md-4">
                    <label class="form-label subtle mb-1" for="compare-benchmark">Benchmark</label>
                    <select id="compare-benchmark" class="form-select bg-dark text-light border-secondary"></select>
                </div>
            </div>
            <div id="compare-results" class="compare-results mt-4"></div>
            <div id="compare-empty" class="subtle mt-3">Need at least two runs with shared benchmark scenarios to compare.</div>
        </section>

        <div id="report-entries">
            <!-- REPORT_ENTRIES -->
        </div>

        <section id="empty-state" class="surface rounded-4 p-4 text-center empty-state">
            <div class="h5 mb-2">No results match the current filters.</div>
            <div class="subtle">Try clearing the search, switching the commit filter, or disabling latest-only mode.</div>
        </section>
    </main>

    <script>
        (() => {
            const runCards = Array.from(document.querySelectorAll('.run-card'));
            const benchmarkCards = Array.from(document.querySelectorAll('.benchmark-card'));
            const searchInput = document.getElementById('filter-search');
            const benchmarkSelect = document.getElementById('filter-benchmark');
            const modeSelect = document.getElementById('filter-mode');
            const commitSelect = document.getElementById('filter-commit');
            const latestCheckbox = document.getElementById('filter-latest');
            const resetButton = document.getElementById('filter-reset');
            const emptyState = document.getElementById('empty-state');
            const comparePanel = document.getElementById('compare-panel');
            const compareLeftRun = document.getElementById('compare-left-run');
            const compareRightRun = document.getElementById('compare-right-run');
            const compareBenchmark = document.getElementById('compare-benchmark');
            const compareResults = document.getElementById('compare-results');
            const compareEmpty = document.getElementById('compare-empty');
            const summaryRuns = document.getElementById('summary-runs');
            const summaryBenchmarks = document.getElementById('summary-benchmarks');
            const summaryCommits = document.getElementById('summary-commits');
            const summaryLatest = document.getElementById('summary-latest');

            const uniqueSorted = values => Array.from(new Set(values.filter(Boolean))).sort((a, b) => a.localeCompare(b));

            function appendOptions(select, values, allLabel) {
                select.innerHTML = '';
                const allOption = document.createElement('option');
                allOption.value = '';
                allOption.textContent = allLabel;
                select.appendChild(allOption);

                uniqueSorted(values).forEach(value => {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value;
                    select.appendChild(option);
                });
            }

            appendOptions(benchmarkSelect, benchmarkCards.map(card => card.dataset.benchmarkName), 'All benchmarks');
            appendOptions(modeSelect, benchmarkCards.map(card => card.dataset.mode), 'All modes');
            appendOptions(commitSelect, runCards.map(card => card.dataset.commit), 'All commits');

            const runOptions = runCards.map(card => ({
                id: card.dataset.runId,
                label: `${card.dataset.timestampLabel} | ${card.dataset.commit}`,
            }));

            compareLeftRun.innerHTML = runOptions.map(x => `<option value="${x.id}">${x.label}</option>`).join('');
            compareRightRun.innerHTML = runOptions.map(x => `<option value="${x.id}">${x.label}</option>`).join('');
            if (runOptions.length > 1) {
                compareRightRun.selectedIndex = 1;
            }
            appendOptions(compareBenchmark, benchmarkCards.map(card => card.dataset.benchmarkName), 'All shared benchmarks');

            function applyFilters() {
                const search = searchInput.value.trim().toLowerCase();
                const benchmark = benchmarkSelect.value;
                const mode = modeSelect.value;
                const commit = commitSelect.value;
                const latestOnly = latestCheckbox.checked;
                const latestRunId = runCards.length > 0 ? runCards[0].dataset.runId : '';

                let visibleBenchmarkCount = 0;
                let visibleRunCount = 0;
                const visibleCommits = new Set();
                let latestVisibleRun = '';

                runCards.forEach(runCard => {
                    const isLatestRun = !latestOnly || runCard.dataset.runId === latestRunId;
                    const commitMatches = !commit || runCard.dataset.commit === commit;
                    const runSearchText = runCard.dataset.search || '';

                    let runHasVisibleBenchmarks = false;
                    const nestedBenchmarks = Array.from(runCard.querySelectorAll('.benchmark-card'));
                    nestedBenchmarks.forEach(card => {
                        const benchmarkMatches = !benchmark || card.dataset.benchmarkName === benchmark;
                        const modeMatches = !mode || card.dataset.mode === mode;
                        const textMatches = !search || card.dataset.search.includes(search) || runSearchText.includes(search);
                        const visible = isLatestRun && commitMatches && benchmarkMatches && modeMatches && textMatches;
                        card.hidden = !visible;
                        if (visible) {
                            runHasVisibleBenchmarks = true;
                            visibleBenchmarkCount++;
                        }
                    });

                    runCard.hidden = !runHasVisibleBenchmarks;
                    if (runHasVisibleBenchmarks) {
                        visibleRunCount++;
                        visibleCommits.add(runCard.dataset.commit);
                        if (!latestVisibleRun) {
                            latestVisibleRun = runCard.dataset.timestampLabel || '-';
                        }
                    }
                });

                summaryRuns.textContent = String(visibleRunCount);
                summaryBenchmarks.textContent = String(visibleBenchmarkCount);
                summaryCommits.textContent = String(visibleCommits.size);
                summaryLatest.textContent = latestVisibleRun || '-';
                emptyState.style.display = visibleBenchmarkCount === 0 ? 'block' : 'none';
            }

            function getRunCard(runId) {
                return document.querySelector(`.run-card[data-run-id="${CSS.escape(runId)}"]`);
            }

            function getBenchmarkCard(runId, benchmarkName) {
                return document.querySelector(`.run-card[data-run-id="${CSS.escape(runId)}"] .benchmark-card[data-benchmark-name="${CSS.escape(benchmarkName)}"]`);
            }

            function parseRows(runId, benchmarkName) {
                return Array.from(document.querySelectorAll(`.run-card[data-run-id="${CSS.escape(runId)}"] .benchmark-card[data-benchmark-name="${CSS.escape(benchmarkName)}"] tbody tr[data-scenario-key]`)).map(row => ({
                    key: row.dataset.scenarioKey,
                    label: row.dataset.scenarioLabel,
                    mode: row.dataset.modeLabel,
                    samples: Number(row.dataset.samples || '0'),
                    allocMin: Number(row.dataset.allocMin || '0'),
                    allocMed: Number(row.dataset.allocMed || '0'),
                    allocAvg: Number(row.dataset.allocAvg || '0'),
                    allocMax: Number(row.dataset.allocMax || '0'),
                    bytesPerOp: Number(row.dataset.bytesPerOp || '0'),
                    timeMed: Number(row.dataset.timeMed || '0')
                }));
            }

            function formatWhole(value) {
                return Number(value).toLocaleString(undefined, { maximumFractionDigits: 0 });
            }

            function formatTime(value) {
                return Number(value).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 3 });
            }

            function getValueClass(leftValue, rightValue, side) {
                if (leftValue === rightValue) {
                    return 'delta-flat';
                }

                if (side === 'left') {
                    return leftValue < rightValue ? 'delta-better' : 'delta-worse';
                }

                return rightValue < leftValue ? 'delta-better' : 'delta-worse';
            }

            function renderRunMetric(value, otherValue, formatter, side) {
                const cssClass = getValueClass(side === 'left' ? value : otherValue, side === 'left' ? otherValue : value, side);
                return `<div class="${cssClass}">${formatter(value)}</div>`;
            }

            function buildCompareBlock(runAId, runBId, benchmarkName) {
                const runACard = getRunCard(runAId);
                const runBCard = getRunCard(runBId);
                const benchmarkACard = getBenchmarkCard(runAId, benchmarkName);
                const benchmarkBCard = getBenchmarkCard(runBId, benchmarkName);
                if (!runACard || !runBCard || !benchmarkACard || !benchmarkBCard) {
                    return '';
                }

                const leftRows = parseRows(runAId, benchmarkName);
                const rightRows = parseRows(runBId, benchmarkName);
                const rightMap = new Map(rightRows.map(row => [row.key, row]));
                const mergedRows = leftRows
                    .filter(row => rightMap.has(row.key))
                    .map(row => ({ left: row, right: rightMap.get(row.key) }));

                if (mergedRows.length === 0) {
                    return '';
                }

                const description = benchmarkACard.dataset.description || benchmarkName;
                const measurement = benchmarkACard.dataset.measurement || '';
                const mode = benchmarkACard.dataset.mode || '';
                const runALabel = `${runACard.dataset.timestampLabel} | ${runACard.dataset.commit}`;
                const runBLabel = `${runBCard.dataset.timestampLabel} | ${runBCard.dataset.commit}`;

                const rowsHtml = mergedRows.map(pair => `
                    <div class="compare-row-group">
                        <div class="compare-run-row">
                            <div><span class="compare-run-chip">A</span></div>
                            <div class="compare-scenario-meta">
                                <div class="fw-semibold">${pair.left.label}</div>
                                <div class="subtle small">Mode: ${pair.left.mode} | Samples: ${pair.left.samples}</div>
                            </div>
                            <div class="compare-run-meta">${runALabel}</div>
                            ${renderRunMetric(pair.left.allocMin, pair.right.allocMin, formatWhole, 'left')}
                            ${renderRunMetric(pair.left.allocMed, pair.right.allocMed, formatWhole, 'left')}
                            ${renderRunMetric(pair.left.allocAvg, pair.right.allocAvg, formatWhole, 'left')}
                            ${renderRunMetric(pair.left.allocMax, pair.right.allocMax, formatWhole, 'left')}
                            ${renderRunMetric(pair.left.bytesPerOp, pair.right.bytesPerOp, formatWhole, 'left')}
                            ${renderRunMetric(pair.left.timeMed, pair.right.timeMed, formatTime, 'left')}
                        </div>
                        <div class="compare-run-row">
                            <div><span class="compare-run-chip">B</span></div>
                            <div class="compare-scenario-meta">
                                <div class="fw-semibold">${pair.right.label}</div>
                                <div class="subtle small">Mode: ${pair.right.mode} | Samples: ${pair.right.samples}</div>
                            </div>
                            <div class="compare-run-meta">${runBLabel}</div>
                            ${renderRunMetric(pair.right.allocMin, pair.left.allocMin, formatWhole, 'right')}
                            ${renderRunMetric(pair.right.allocMed, pair.left.allocMed, formatWhole, 'right')}
                            ${renderRunMetric(pair.right.allocAvg, pair.left.allocAvg, formatWhole, 'right')}
                            ${renderRunMetric(pair.right.allocMax, pair.left.allocMax, formatWhole, 'right')}
                            ${renderRunMetric(pair.right.bytesPerOp, pair.left.bytesPerOp, formatWhole, 'right')}
                            ${renderRunMetric(pair.right.timeMed, pair.left.timeMed, formatTime, 'right')}
                        </div>
                        <div class="compare-divider"></div>
                    </div>`).join('');

                return `
                    <article class="benchmark-card rounded-4 p-4">
                        <div class="d-flex flex-column flex-xl-row justify-content-between gap-3 mb-3">
                            <div>
                                <h3 class="h5 mb-1">${benchmarkName}</h3>
                                <p class="subtle mb-2">${description}</p>
                                <div class="d-flex flex-wrap gap-2">
                                    <span class="pill">Mode: ${mode}</span>
                                    <span class="pill">Measurement: ${measurement}</span>
                                </div>
                            </div>
                        </div>
                        <div class="compare-grid-head">
                            <div>Run</div>
                            <div>Scenario</div>
                            <div>Run Meta</div>
                            <div>Alloc min</div>
                            <div>Alloc med</div>
                            <div>Alloc avg</div>
                            <div>Alloc max</div>
                            <div>B/op med</div>
                            <div>Time med</div>
                        </div>
                        <div class="compare-table">${rowsHtml}</div>
                    </article>`;
            }

            function renderCompare() {
                const leftRunId = compareLeftRun.value;
                const rightRunId = compareRightRun.value;
                const benchmarkFilter = compareBenchmark.value;

                comparePanel.style.display = runOptions.length > 1 ? 'block' : 'none';
                if (!leftRunId || !rightRunId) {
                    compareResults.innerHTML = '';
                    compareEmpty.textContent = 'Need at least two runs with shared benchmark scenarios to compare.';
                    return;
                }

                if (leftRunId === rightRunId) {
                    compareResults.innerHTML = '';
                    compareEmpty.textContent = 'Pick two different runs to compare.';
                    return;
                }

                const leftNames = new Set(Array.from(document.querySelectorAll(`.run-card[data-run-id="${CSS.escape(leftRunId)}"] .benchmark-card`)).map(card => card.dataset.benchmarkName));
                const rightNames = new Set(Array.from(document.querySelectorAll(`.run-card[data-run-id="${CSS.escape(rightRunId)}"] .benchmark-card`)).map(card => card.dataset.benchmarkName));
                let sharedBenchmarkNames = Array.from(leftNames).filter(name => rightNames.has(name)).sort((a, b) => a.localeCompare(b));

                if (benchmarkFilter) {
                    sharedBenchmarkNames = sharedBenchmarkNames.filter(name => name === benchmarkFilter);
                }

                const blocks = sharedBenchmarkNames
                    .map(name => buildCompareBlock(leftRunId, rightRunId, name))
                    .filter(Boolean);

                compareResults.innerHTML = blocks.join('');
                compareEmpty.textContent = blocks.length > 0
                    ? ''
                    : benchmarkFilter
                        ? 'These runs do not share matching scenarios for the selected benchmark.'
                        : 'These runs do not share matching benchmark scenarios.';
            }

            [searchInput, benchmarkSelect, modeSelect, commitSelect, latestCheckbox].forEach(control => {
                control.addEventListener('input', applyFilters);
                control.addEventListener('change', applyFilters);
            });

            [compareLeftRun, compareRightRun, compareBenchmark].forEach(control => {
                control.addEventListener('input', renderCompare);
                control.addEventListener('change', renderCompare);
            });

            resetButton.addEventListener('click', () => {
                searchInput.value = '';
                benchmarkSelect.value = '';
                modeSelect.value = '';
                commitSelect.value = '';
                latestCheckbox.checked = false;
                applyFilters();
            });

            applyFilters();
            renderCompare();
        })();
    </script>
</body>
</html>
""";
    }

    private static string BuildReportEntry(HarnessRunReport report)
    {
        var runId = report.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        var timestampLabel = report.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        var runSearch = $"{report.GitDescription} {report.DotNetVersion} {report.OsDescription}".ToLowerInvariant();

        var builder = new StringBuilder();
        builder.AppendLine($"""<section class="run-card rounded-4 p-4 mb-4" data-run-id="{Encode(runId)}" data-commit="{Encode(report.GitDescription)}" data-timestamp-label="{Encode(timestampLabel)}" data-search="{Encode(runSearch)}">""");
        builder.AppendLine("""  <div class="d-flex flex-column flex-lg-row justify-content-between gap-3 mb-3">""");
        builder.AppendLine("""    <div>""");
        builder.AppendLine($"""      <h2 class="h4 mb-1">Run <span class="subtle">{Encode(timestampLabel)}</span></h2>""");
        builder.AppendLine("""      <div class="d-flex flex-wrap gap-2 mt-2">""");
        builder.AppendLine($"""        <span class="pill">Commit: {Encode(report.GitDescription)}</span>""");
        builder.AppendLine($"""        <span class="pill">.NET: {Encode(report.DotNetVersion)}</span>""");
        builder.AppendLine($"""        <span class="pill">OS: {Encode(report.OsDescription)}</span>""");
        builder.AppendLine($"""        <span class="pill">Benchmarks: {report.Benchmarks.Count.ToString(CultureInfo.InvariantCulture)}</span>""");
        builder.AppendLine("""      </div>""");
        builder.AppendLine("""    </div>""");
        builder.AppendLine("""  </div>""");

        foreach (var benchmarkRun in report.Benchmarks)
            builder.Append(BuildBenchmarkSection(benchmarkRun, report.GitDescription, timestampLabel));

        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildBenchmarkSection(BenchmarkRunResult run, string commit, string timestampLabel)
    {
        var builder = new StringBuilder();
        var benchmark = run.Benchmark;
        var mode = FormatMode(run.Options.Mode);
        var searchText = string.Join(' ',
            benchmark.Name,
            benchmark.Description,
            mode,
            commit,
            timestampLabel,
            benchmark.MeasurementDescription).ToLowerInvariant();

        builder.AppendLine($"""<article class="benchmark-card rounded-4 p-4 mb-4" data-benchmark-name="{Encode(benchmark.Name)}" data-mode="{Encode(mode)}" data-commit="{Encode(commit)}" data-description="{Encode(benchmark.Description)}" data-measurement="{Encode(benchmark.MeasurementDescription)}" data-search="{Encode(searchText)}">""");
        builder.AppendLine("""  <div class="d-flex flex-column flex-xl-row justify-content-between gap-3 mb-3">""");
        builder.AppendLine("""    <div>""");
        builder.AppendLine($"""      <h3 class="h5 mb-1">{Encode(benchmark.Name)}</h3>""");
        builder.AppendLine($"""      <p class="subtle mb-2">{Encode(benchmark.Description)}</p>""");
        builder.AppendLine("""      <div class="d-flex flex-wrap gap-2">""");
        builder.AppendLine($"""        <span class="pill">Mode: {Encode(mode)}</span>""");
        builder.AppendLine($"""        <span class="pill">Samples: {run.Options.Samples.ToString(CultureInfo.InvariantCulture)}</span>""");
        builder.AppendLine($"""        <span class="pill">Measurement: {Encode(benchmark.MeasurementDescription)}</span>""");
        builder.AppendLine("""      </div>""");
        builder.AppendLine("""    </div>""");
        builder.AppendLine("""  </div>""");

        builder.AppendLine("""  <div class="table-responsive mb-3">""");
        builder.AppendLine("""    <table class="table table-sm align-middle mb-0">""");
        builder.AppendLine("""      <thead><tr>""");
        builder.AppendLine($"""        <th>Mode</th><th>{Encode(benchmark.ParameterNames[0])}</th><th>{Encode(benchmark.ParameterNames[1])}</th><th>{Encode(benchmark.ParameterNames[2])}</th><th>Samples</th><th>Alloc min</th><th>Alloc med</th><th>Alloc avg</th><th>Alloc max</th><th>B/op med</th><th>Time med (ms)</th>""");
        builder.AppendLine("""      </tr></thead>""");
        builder.AppendLine("""      <tbody>""");

        foreach (var result in run.ScenarioResults)
        {
            var bytesPerOp = result.AllocationStats.Median / GetOperationCount(result.Scenario, benchmark);
            var scenarioKey = $"{result.Scenario.P1}|{result.Scenario.P2}|{result.Scenario.P3}";
            var scenarioLabel = $"{benchmark.ParameterNames[0]}={result.Scenario.P1}, {benchmark.ParameterNames[1]}={result.Scenario.P2}, {benchmark.ParameterNames[2]}={result.Scenario.P3}";
            builder.AppendLine($"""        <tr data-scenario-key="{Encode(scenarioKey)}" data-scenario-label="{Encode(scenarioLabel)}" data-mode-label="{Encode(FormatScenarioMode(result.Mode))}" data-samples="{result.SampleResults.Count.ToString(CultureInfo.InvariantCulture)}" data-alloc-min="{result.AllocationStats.Min.ToString("0.###", CultureInfo.InvariantCulture)}" data-alloc-med="{result.AllocationStats.Median.ToString("0.###", CultureInfo.InvariantCulture)}" data-alloc-avg="{result.AllocationStats.Average.ToString("0.###", CultureInfo.InvariantCulture)}" data-alloc-max="{result.AllocationStats.Max.ToString("0.###", CultureInfo.InvariantCulture)}" data-bytes-per-op="{bytesPerOp.ToString("0.###", CultureInfo.InvariantCulture)}" data-time-med="{result.ElapsedStats.Median.ToString("0.###", CultureInfo.InvariantCulture)}">""");
            builder.AppendLine($"""          <td>{Encode(FormatScenarioMode(result.Mode))}</td>""");
            builder.AppendLine($"""          <td>{result.Scenario.P1.ToString(CultureInfo.InvariantCulture)}</td>""");
            builder.AppendLine($"""          <td>{result.Scenario.P2.ToString(CultureInfo.InvariantCulture)}</td>""");
            builder.AppendLine($"""          <td>{result.Scenario.P3.ToString(CultureInfo.InvariantCulture)}</td>""");
            builder.AppendLine($"""          <td>{result.SampleResults.Count.ToString(CultureInfo.InvariantCulture)}</td>""");
            builder.AppendLine($"""          <td>{FormatWholeNumber(result.AllocationStats.Min)}</td>""");
            builder.AppendLine($"""          <td>{FormatWholeNumber(result.AllocationStats.Median)}</td>""");
            builder.AppendLine($"""          <td>{FormatWholeNumber(result.AllocationStats.Average)}</td>""");
            builder.AppendLine($"""          <td>{FormatWholeNumber(result.AllocationStats.Max)}</td>""");
            builder.AppendLine($"""          <td>{FormatWholeNumber(bytesPerOp)}</td>""");
            builder.AppendLine($"""          <td>{result.ElapsedStats.Median.ToString("0.###", CultureInfo.InvariantCulture)}</td>""");
            builder.AppendLine("""        </tr>""");
        }

        builder.AppendLine("""      </tbody>""");
        builder.AppendLine("""    </table>""");
        builder.AppendLine("""  </div>""");

        var comparisons = BuildComparisonRows(run.ScenarioResults);
        if (comparisons.Count > 0)
        {
            builder.AppendLine("""  <div class="metric-card rounded-4 p-3">""");
            builder.AppendLine("""    <div class="fw-semibold mb-2">Legacy-simulated vs checked-out</div>""");
            builder.AppendLine("""    <div class="table-responsive">""");
            builder.AppendLine("""      <table class="table table-sm align-middle mb-0">""");
            builder.AppendLine("""        <thead><tr>""");
            builder.AppendLine($"""          <th>{Encode(benchmark.ParameterNames[0])}</th><th>{Encode(benchmark.ParameterNames[1])}</th><th>{Encode(benchmark.ParameterNames[2])}</th><th>Alloc ratio (L/C)</th><th>Time ratio (L/C)</th>""");
            builder.AppendLine("""        </tr></thead>""");
            builder.AppendLine("""        <tbody>""");
            foreach (var comparison in comparisons)
            {
                builder.AppendLine("""          <tr>""");
                builder.AppendLine($"""            <td>{comparison.Scenario.P1.ToString(CultureInfo.InvariantCulture)}</td>""");
                builder.AppendLine($"""            <td>{comparison.Scenario.P2.ToString(CultureInfo.InvariantCulture)}</td>""");
                builder.AppendLine($"""            <td>{comparison.Scenario.P3.ToString(CultureInfo.InvariantCulture)}</td>""");
                builder.AppendLine($"""            <td class="{GetRatioClass(comparison.AllocationRatio)}">{comparison.AllocationRatio.ToString("0.##", CultureInfo.InvariantCulture)}x</td>""");
                builder.AppendLine($"""            <td class="{GetRatioClass(comparison.ElapsedRatio)}">{comparison.ElapsedRatio.ToString("0.##", CultureInfo.InvariantCulture)}x</td>""");
                builder.AppendLine("""          </tr>""");
            }
            builder.AppendLine("""        </tbody>""");
            builder.AppendLine("""      </table>""");
            builder.AppendLine("""    </div>""");
            builder.AppendLine("""  </div>""");
        }

        builder.AppendLine("""</article>""");
        return builder.ToString();
    }

    private static List<ComparisonRow> BuildComparisonRows(IReadOnlyList<ScenarioResult> results)
    {
        return results
            .GroupBy(x => x.Scenario)
            .Select(group => new
            {
                Scenario = group.Key,
                CheckedOut = group.FirstOrDefault(x => x.Mode == ScenarioMode.CheckedOut),
                Legacy = group.FirstOrDefault(x => x.Mode == ScenarioMode.LegacySimulated)
            })
            .Where(x => x.CheckedOut is not null && x.Legacy is not null)
            .Select(x => new ComparisonRow(
                x.Scenario,
                x.Legacy!.AllocationStats.Median / x.CheckedOut!.AllocationStats.Median,
                x.Legacy.ElapsedStats.Median / x.CheckedOut.ElapsedStats.Median))
            .ToList();
    }

    private static string GetRatioClass(double ratio)
    {
        return ratio >= 1.0d ? "ratio-warn" : "ratio-good";
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

    private static double GetOperationCount(Scenario scenario, BenchmarkDefinition benchmark)
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

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private sealed record ComparisonRow(Scenario Scenario, double AllocationRatio, double ElapsedRatio);
}
