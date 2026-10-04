# Fastjobs
![CI](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml/badge.svg)

Welcome to the Quick Start Guide for Getting up and running with **FastJobs** .

FastJobs is a lightweight .NET background job processing library built for simplicity and speed.

## Features
- **Job types:** fire-and-forget (enqueued), delayed/scheduled, and recurring jobs (interval or cron), defined from lambda expressions or `IBackGroundJob` classes
- **Chained jobs:** run jobs in sequence with `CreateChain(...)` and `ThenRun(...)`
- **After actions:** run follow-up actions once a job finishes; for recurring jobs, run per instance or only on final completion
- **Scheduler tuning:** configurable idle wait and max sleep
- **UTC-safe dates:** all timestamps use `DateTimeOffset` and are stored as UTC across every provider
- **Retries:** configurable max retries with exponential backoff and jitter, plus `TerminateJobException` to fail a job immediately without retrying
- **Expiration:** jobs, including recurring jobs, move to an `Expired` state once they pass their expiry, which releases their queue entry and resource lock
- **Misfire handling:** `Skip`, `FireOnce` and `Smart` misfire policies for recurring jobs, with a background misfire detector
- **Orphaned recurring job recovery:** a sweeper reschedules recurring jobs that are no longer tracked by the scheduling pipeline
- **Database cleanup:** pluggable pruning strategies for completed and expired jobs (off by default)
- **Worker observability:** worker heartbeats and state tracking
- **Web dashboard:** optional Blazor dashboard for jobs, workers and summary metrics, with periodic auto-refresh
- **Pluggable persistence:** storage is provider based (see below), with automatic schema initialization

## Supported Databases
| Database | Package | Dependency class |
|---|---|---|
| MariaDB / MySQL | `FastJobs.MariaDB` | `FastJobMysqlDependencies` |
| Microsoft SQL Server | `FastJobs.SqlServer` | `FastJobMSSQLDependencies` |
| PostgreSQL | `FastJobs.PostgreSQL` | `FastJobPostgresDependencies` |

More providers are planned. All providers are covered by integration tests that run against real databases using Testcontainers.

Full documentation lives in the [docs](docs/index.md) folder (quickstart, configuration, enqueued/delayed/recurring jobs, after actions, monitoring).

As you read this guide, expect to see details of:
- Fastjobs Installation
- Configuration & Setup


## Fastjobs Installation
You can install Fastjobs via the .NET CLI or the NuGet Package Manager.

### .NET CLI

```bash
dotnet add package FastJobs
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package FastJobs
```



## NuGet Packages

FastJobs is split into focused packages so you only install what you need.


`FastJobs`  Core engine already discussed above and required for all setups 

`FastJobs.Persistence`  Shared persistence abstractions and models used by the core engine and the providers (pulled in by the providers)

`FastJobs.MariaDB`  Persistence provider for MariaDB / MySQL

```bash
dotnet add package FastJobs.MariaDB
```

`FastJobs.SqlServer`  Persistence provider for Microsoft SQL Server

```bash
dotnet add package FastJobs.SqlServer
```

`FastJobs.PostgreSQL`  Persistence provider for PostgreSQL

```bash
dotnet add package FastJobs.PostgreSQL
```

Install one persistence provider alongside the core package.

`FastJobs.Dashboard` Optional RCL dashboard for monitoring and  observability 

```bash
dotnet add package FastJobs.Dashboard
```


---

## Configuration & Setup
Fastjobs Is Very Easy To Setup And Get Going. The main job scheduling services you will be interacting with live in the `FastJobs` namespace and the database providers in the `FastJobs.Persistence` namespace.

To use Fastjobs Add the following using statements 
``` csharp
using FastJobs;
using FastJobs.Persistence;
```

Next Call `builder.Services.AddFastJobs()` with Options for extra config info like so 

```csharp 
string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// MariaDB / MySQL (FastJobs.MariaDB)
builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 4; },
    new FastJobMysqlDependencies(
        options => options.ConnectionString = connectionString
    )
);

// ...or Microsoft SQL Server (FastJobs.SqlServer)
// builder.Services.AddFastJobs(
//     option => {  option.WorkerCount = 4; },
//     new FastJobMSSQLDependencies(
//         options => options.ConnectionString = connectionString
//     )
// );

// ...or PostgreSQL (FastJobs.PostgreSQL)
// builder.Services.AddFastJobs(
//     option => {  option.WorkerCount = 4; },
//     new FastJobPostgresDependencies(
//         options => options.ConnectionString = connectionString
//     )
// );

//TO INCLUDE THE WEB DASHBOARD
builder.Services.AddFastjobsDashboard();

```

to Finish up configuration call use FastJobs()
```csharp

var app = builder.Build();

app.Services.UseFastJobs();
```
---
if you would like to include the Web Dashboard NB: This Wont work if your application does not use a Web host

```csharp
//ADD USING STATEMENT 
using FastJobs.Dashboard;

/*{
    DI And Services Setup
}*/

var app = builder.Build();

app.UseFastJobsDashboard("/Dashboard"); //should come before routing is done to allow rewriting path to internal Dashboard path
app.UseStaticFiles();   
app.UseRouting();
app.UseAntiforgery();   

//Expose Dashboard Components
app.MapFastJobsDashboard();


```