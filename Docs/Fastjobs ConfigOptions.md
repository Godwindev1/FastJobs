Here's the refined documentation:

---

# FastJobs Configuration

## Global Configuration

FastJobs is configured at startup by passing a `FastJobsOptions` instance (and an optional storage dependency) to `AddFastJobs`:

```csharp
builder.Services.AddFastJobs(
    options => { options.WorkerCount = 2; },
    new FastJobMysqlDependencies(opts => opts.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);
```

## FastJobsOptions Reference

All available global options and their defaults:

```csharp
var options = new FastJobsOptions
{
    WorkerCount = 2,
    MaxSleep = TimeSpan.FromSeconds(10),
    DefaultWorkerHeartbeatIntervalSeconds = 30,
    IdleWaitPeriod = TimeSpan.FromSeconds(30),
    DefaultJobExpiration = TimeSpan.FromHours(24),
    DefaultMaxRetries = 4
};
```

## Storage Configuration

Storage is configured via a provider-specific dependency class. For MySQL:

```csharp
// Inline (recommended)
builder.Services.AddFastJobs(
    options => { options.WorkerCount = 2; },
    new FastJobMysqlDependencies(opts =>
        opts.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);

// Or constructed separately
var sqlOptions = new FastJobsSqlStorageOptions
{
    ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;"
};
```

## Per-Job Configuration

Job-level settings (retries, expiration, etc.) are configured using a **fluent builder pattern** at enqueue time. See the [Job Enqueueing](#) section for details.

---

Main changes: fixed the typo `DefaultWOrkerHeartbeatIntervalSeconds` → `DefaultWorkerHeartbeatIntervalSeconds`, removed redundant code blocks, added a brief description to each section, and tightened the overall structure. Let me know if you'd like any section expanded or the tone adjusted.