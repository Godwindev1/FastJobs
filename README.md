<div align="center">

# FastJobs

**Lightweight background job processing for .NET, built for simplicity and speed.**
Fire-and-forget, delayed and recurring jobs with database-backed persistence and an optional Blazor dashboard.

[![CI](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml/badge.svg)](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml)

[Website](https://fastjobs.tech) · [Documentation](docs/index.md) · [NuGet](https://www.nuget.org/packages/FastJobs) · [Issues](https://github.com/Godwindev1/FastJobs/issues)

</div>

<p align="center">
  <img src="https://fastjobs.tech/assets/Dashboard.png" alt="The FastJobs dashboard showing jobs and summary metrics" width="900" />
</p>

## ✨ Features

### ⚙️ Job types
- **Fire-and-forget, delayed/scheduled and recurring jobs** (interval or cron), defined from lambda expressions or `IBackGroundJob` classes.
- **Chained jobs:** run jobs in sequence with `CreateChain(...)` and `ThenRun(...)`. Steps never overlap, and this is verified in tests.
- **After actions:** run follow-up actions once a job finishes. For recurring jobs, run per instance or only on final completion.

### 🛡️ Reliability
- **Retries with exponential backoff and jitter:** configurable base delay (default 10s), max delay cap (default 300s) and jitter (default 5s), so failing jobs don't retry in lockstep. Throw `TerminateJobException` to fail a job immediately without retrying.
- **Misfire handling:** `Skip`, `FireOnce` and `Smart` policies for recurring jobs, with a background misfire detector for jobs that missed their scheduled time.
- **Drift-free recurring jobs:** the next occurrence is scheduled from the scheduled time, and missed runs are skipped.
- **Per-job-type concurrency limits:** one job type can't saturate the worker pool.
- **Expiration:** jobs, including recurring ones, move to an `Expired` state after their expiry, releasing their queue entry and resource lock.
- **Orphaned recurring job recovery:** a sweeper reschedules recurring jobs that the scheduling pipeline no longer tracks.
- **UTC-safe dates:** all timestamps use `DateTimeOffset` and are stored as UTC across every provider.
- **Scheduler tuning:** configurable idle wait and max sleep.

### 🧹 Cleanup strategies
Database pruning is pluggable and **off by default**. A `BackgroundService` runs your chosen `ICleanupStrategy` on an interval.

| Strategy | Behavior |
|---|---|
| `CompletedJobsPruningStrategy` | Prunes completed jobs |
| `ExpiredJobsPruningStrategy` | Prunes expired jobs |
| `NullStrategy` | Does nothing |

- **Custom strategies:** implement `ICleanupStrategy` with full DI access to encode your own retention, archival or compliance rules.
- **Safe by design:** `CancellationToken` is supported end to end, and a faulty strategy is caught and logged, so it can't crash the host.

### 📊 Observability
- **Blazor dashboard** (`FastJobs.Dashboard`) for jobs, workers and summary metrics, with periodic auto-refresh. Shipped as a Razor class library with embedded static assets and path-rewriting middleware.
- **Worker heartbeats and state tracking.**
- **Structured logging through `ILogger`:** plug in Serilog, NLog or any other sink.
- **Metrics** are covered by tests using `Microsoft.Extensions.Diagnostics.Testing`.

<p align="center">
  <img src="https://fastjobs.tech/assets/Workers.png" alt="The FastJobs dashboard showing worker status and heartbeats" width="900" />
</p>

### 🗄️ Decoupled storage
The core has **no database dependency**. Storage is a swappable provider package with automatic schema initialization.

| Database | Package | Dependency class |
|---|---|---|
| MariaDB / MySQL | `FastJobs.MariaDB` | `FastJobMysqlDependencies` |
| Microsoft SQL Server | `FastJobs.SqlServer` | `FastJobMSSQLDependencies` |
| PostgreSQL | `FastJobs.PostgreSQL` | `FastJobPostgresDependencies` |

More providers are planned.

## 🧪 Testing

- **Real databases:** the xUnit integration suite runs against real databases through Testcontainers, not mocks.
- **Contract tests:** one abstract base suite runs against every provider, guaranteeing identical behavior.
- **Solid fixtures:** `IClassFixture`, `ICollectionFixture` and serialized collections handle shared static state.
- **Covered behavior:** pruning, misfire detection, job chaining and metrics.

See [`TESTING.md`](TESTING.md) for how to run the suite.

## 📦 Delivery

- **Independently versioned NuGet packages** (`.nupkg` plus `.snupkg` symbols): `FastJobs`, `FastJobs.Persistence`, `FastJobs.Dashboard`, `FastJobs.SqlServer`, `FastJobs.MariaDB`, `FastJobs.PostgreSQL`.
- **GitHub Actions:** CI on every push to `main`, and NuGet publish on version tags with automatic semver extraction.
- **Docs and demo site:** [fastjobs.tech](https://fastjobs.tech).

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

The scheduling services live in the `FastJobs` namespace and the database providers in `FastJobs.Persistence`.

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

## 📚 Documentation

Full documentation is in the [docs](docs/index.md) folder: quickstart, configuration, enqueued/delayed/recurring jobs, after actions and monitoring. Docs and demo site: [fastjobs.tech](https://fastjobs.tech).
