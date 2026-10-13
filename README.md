<div align="center">

# FastJobs

**Lightweight background job processing for .NET, built for simplicity and speed.**
Fire-and-forget, delayed and recurring jobs with database-backed persistence and an optional Blazor dashboard.

[![CI](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml/badge.svg)](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml)

[Website](https://fastjobs.tech) · [Documentation](docs/index.md) · [NuGet](https://www.nuget.org/packages/FastJobs) · [Issues](https://github.com/Godwindev1/FastJobs/issues)

</div>

<!-- HERO IMAGE: replace the src below with the first image URL from fastjobs.tech, then remove these comment markers.
<p align="center">
  <img src="HERO_IMAGE_URL" alt="FastJobs overview" width="900" />
</p>
-->

## ✨ Features

### ⚙️ Job types
- **Fire-and-forget, delayed/scheduled and recurring jobs** (interval or cron), defined from lambda expressions or `IBackGroundJob` classes.
- **Chained jobs:** run jobs in sequence with `CreateChain(...)` and `ThenRun(...)`.
- **After actions:** run follow-up actions once a job finishes. For recurring jobs, run per instance or only on final completion.

### 🛡️ Reliability
- **Retries:** configurable max retries with exponential backoff and jitter. Throw `TerminateJobException` to fail a job immediately without retrying.
- **Misfire handling:** `Skip`, `FireOnce` and `Smart` policies for recurring jobs, with a background misfire detector.
- **Expiration:** jobs, including recurring ones, move to an `Expired` state after their expiry, releasing their queue entry and resource lock.
- **Orphaned recurring job recovery:** a sweeper reschedules recurring jobs that the scheduling pipeline no longer tracks.
- **UTC-safe dates:** all timestamps use `DateTimeOffset` and are stored as UTC across every provider.

### 📊 Operations
- **Web dashboard:** optional Blazor dashboard for jobs, workers and summary metrics, with periodic auto-refresh.
- **Worker observability:** worker heartbeats and state tracking.
- **Database cleanup:** pluggable pruning strategies for completed and expired jobs (off by default).
- **Scheduler tuning:** configurable idle wait and max sleep.

### 🗄️ Storage
- **Pluggable persistence:** provider-based storage with automatic schema initialization.
- **Tested against real databases:** all providers are covered by integration tests using Testcontainers.

<!-- DASHBOARD IMAGE: replace the src below with the second image URL from fastjobs.tech, then remove these comment markers.
### 📊 Dashboard

<p align="center">
  <img src="DASHBOARD_IMAGE_URL" alt="The FastJobs dashboard" width="900" />
</p>
-->

## 🗄️ Supported Databases

| Database | Package | Dependency class |
|---|---|---|
| MariaDB / MySQL | `FastJobs.MariaDB` | `FastJobMysqlDependencies` |
| Microsoft SQL Server | `FastJobs.SqlServer` | `FastJobMSSQLDependencies` |
| PostgreSQL | `FastJobs.PostgreSQL` | `FastJobPostgresDependencies` |

More providers are planned.

## 🚀 Quick Start

### 1. Install

FastJobs is split into focused packages, so you only install what you need. Add the core package and **one** persistence provider:

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
| `FastJobs.Dashboard` | Optional Razor class library dashboard for monitoring |

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
