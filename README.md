<div align="center">

# FastJobs

**Lightweight background job processing for .NET, built for simplicity and speed.**
Fire-and-forget, delayed and recurring jobs with database-backed persistence and an optional Blazor dashboard.

[![CI](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml/badge.svg)](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml)

[Website](https://fastjobs.tech) · [Documentation](https://godwindev1.github.io/FastJobs) · [NuGet](https://www.nuget.org/packages/FastJobs) · [Issues](https://github.com/Godwindev1/FastJobs/issues)

</div>

<p align="center">
  <img src="https://fastjobs.tech/assets/Dashboard.png" alt="The FastJobs dashboard showing jobs and summary metrics" width="900" />
</p>

## 🚀 Quick Start

### 1. Install

Add the core package and **one** persistence provider:

```bash
dotnet add package FastJobs
dotnet add package FastJobs.PostgreSQL   # or FastJobs.SqlServer / FastJobs.MariaDB
dotnet add package FastJobs.Dashboard    # optional
```

| Package | Purpose |
|---|---|
| `FastJobs` | Core engine, required for all setups |
| `FastJobs.Persistence` | Shared abstractions and models (pulled in by the providers) |
| `FastJobs.MariaDB` | Provider for MariaDB / MySQL |
| `FastJobs.SqlServer` | Provider for Microsoft SQL Server |
| `FastJobs.PostgreSQL` | Provider for PostgreSQL |
| `FastJobs.Dashboard` | Optional Razor class library dashboard |

Prefer Visual Studio? Use `Install-Package FastJobs` in the Package Manager Console.

### 2. Register the services

```csharp
using FastJobs;
using FastJobs.Persistence;

string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// PostgreSQL (FastJobs.PostgreSQL)
builder.Services.AddFastJobs(
    option => { option.WorkerCount = 4; },
    new FastJobPostgresDependencies(
        options => options.ConnectionString = connectionString
    )
);

// ...or Microsoft SQL Server (FastJobs.SqlServer)
// builder.Services.AddFastJobs(
//     option => { option.WorkerCount = 4; },
//     new FastJobMSSQLDependencies(
//         options => options.ConnectionString = connectionString
//     )
// );

// ...or MariaDB / MySQL (FastJobs.MariaDB)
// builder.Services.AddFastJobs(
//     option => { option.WorkerCount = 4; },
//     new FastJobMysqlDependencies(
//         options => options.ConnectionString = connectionString
//     )
// );
```

### 3. Start the engine

```csharp
var app = builder.Build();

app.Services.UseFastJobs();
```

### 4. Add the dashboard (optional)

> [!NOTE]
> The dashboard requires a web host. It will not work in applications without one.

Register the dashboard services alongside the setup above:

```csharp
builder.Services.AddFastjobsDashboard();
```

Then wire it into the pipeline:

```csharp
using FastJobs.Dashboard;

var app = builder.Build();

app.UseFastJobsDashboard("/Dashboard"); // must come before routing so the path can be rewritten to the internal dashboard path
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Expose dashboard components
app.MapFastJobsDashboard();
```

## ✨ Features

- **Job types:** fire-and-forget (enqueued), delayed/scheduled, and recurring jobs (interval or cron), defined from lambda expressions or `IBackGroundJob` classes
- **Chained jobs:** run jobs in sequence with `CreateChain(...)` and `ThenRun(...)`
- **After actions:** run follow-up actions once a job finishes; for recurring jobs, run per instance or only on final completion
- **Retries:** configurable max retries with exponential backoff and jitter (defaults: 10s base, 300s cap, 5s jitter), plus `TerminateJobException` to fail a job immediately without retrying
- **Misfire handling:** `Skip`, `FireOnce` and `Smart` policies for recurring jobs, with a background misfire detector
- **Concurrency limits:** per-job-type limits so one job type can't saturate the worker pool
- **Expiration:** jobs, including recurring jobs, move to an `Expired` state once they pass their expiry, which releases their queue entry and resource lock
- **Orphaned recurring job recovery:** a sweeper reschedules recurring jobs that are no longer tracked by the scheduling pipeline
- **UTC-safe dates:** all timestamps use `DateTimeOffset` and are stored as UTC across every provider
- **Scheduler tuning:** configurable idle wait and max sleep
- **Observability:** worker heartbeats, state tracking, and structured `ILogger` logging (plug in Serilog, NLog, etc.)
- **Web dashboard:** optional Blazor dashboard for jobs, workers and summary metrics, with periodic auto-refresh
- **Pluggable persistence:** provider-based storage with automatic schema initialization

<p align="center">
  <img src="https://fastjobs.tech/assets/Workers.png" alt="The FastJobs dashboard showing worker status and heartbeats" width="900" />
</p>

### 🗄️ Supported databases

| Database | Package | Dependency class |
|---|---|---|
| MariaDB / MySQL | `FastJobs.MariaDB` | `FastJobMysqlDependencies` |
| Microsoft SQL Server | `FastJobs.SqlServer` | `FastJobMSSQLDependencies` |
| PostgreSQL | `FastJobs.PostgreSQL` | `FastJobPostgresDependencies` |

More providers are planned.

### 🧹 Cleanup

Pruning is pluggable and **off by default**. Built-in strategies are `CompletedJobsPruningStrategy`, `ExpiredJobsPruningStrategy` and `NullStrategy`. Implement `ICleanupStrategy` (with full DI access) to write your own retention or archival rules.

### 🧪 Testing

All providers are covered by integration tests that run against real databases using Testcontainers, with one shared contract suite guaranteeing identical behavior across providers.

## 🔧 Under the hood

- **Decoupled core:** no database dependency; storage is a swappable provider package.
- **Fault-tolerant background work:** a faulty cleanup strategy is caught and logged, and can't crash the host.
- **Automated releases:** independently versioned NuGet packages with symbols, built and published by GitHub Actions on version tags.

For the full design, see [ARCHITECTURE.md](ARCHITECTURE.md).

## 📚 Documentation

Full documentation is in the [docs](docs/index.md) folder: quickstart, configuration, enqueued/delayed/recurring jobs, after actions and monitoring. Docs and demo site: [fastjobs.tech](https://fastjobs.tech).
