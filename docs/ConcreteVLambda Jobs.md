---
title: Concrete v Lambda Jobs
nav_order: 7
next_page:
  title: Monitoring
  url: /Monitoring
---

# Concrete Jobs vs Lambda Jobs

FastJobs supports two ways to define and enqueue background jobs: **Concrete Jobs** and **Lambda Jobs**.
Both produce the same outcome — a job that runs in the background — but they differ in how they are
defined, what capabilities they have, and their performance characteristics.

---

## Concrete Jobs

A concrete job is a class that implements the `IBackgroundJob` interface. It is the primary and
recommended way to define jobs in FastJobs.

```csharp
public class ComplexTestJob : IBackgroundJob
{
    private readonly ILogger<ComplexTestJob> _logger;

    public ComplexTestJob(ILogger<ComplexTestJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ComplexTestJob started at {Time}", DateTime.UtcNow);
        await Task.Delay(5000);
        _logger.LogInformation("ComplexTestJob completed at {Time}", DateTime.UtcNow);
    }
}
```

### Enqueueing a concrete job

```csharp
await FastJobServer.EnqueueJob<ComplexTestJob>().Start();
```

### Characteristics

- Supports **dependency injection** — services registered in your DI container (loggers, repositories,
  HTTP clients, etc.) are automatically injected via the constructor.
- Supports `CancellationToken` propagation for cooperative cancellation.
- Recommended for jobs that require external services or have non-trivial logic.
- No measurable overhead beyond the job's own execution.

---

## Lambda Jobs

A lambda job is a short inline action passed directly to `EnqueueJob`. Internally, FastJobs wraps it
in a concrete job adapter using expression trees.

```csharp
await FastJobServer.EnqueueJob(() => Console.WriteLine("Hello World")).Start();
```

### Characteristics

- **Does not support dependency injection.** Only values captured in the closure are available.
- Best suited for simple, self-contained fire-and-forget tasks.
- Carries a slightly higher overhead than concrete jobs due to the expression tree and adapter
  indirection used internally. The practical impact of this has not yet been benchmarked — it may
  be negligible depending on job volume and frequency.

---

## Comparison

| | Concrete job | Lambda job |
|---|---|---|
| Definition | Class implementing `IBackgroundJob` | Inline `Action` or `Func` |
| Dependency injection | ✅ Supported | ❌ Not supported |
| `CancellationToken` support | ✅ Yes | ❌ No |
| Performance overhead | Baseline | Slightly higher (unmeasured) |
| Recommended for | Most jobs | Simple, one-off tasks |

---

## When to use which

Use a **concrete job** when:
- Your job depends on injected services (database, logger, HTTP client, etc.).
- The job logic is complex enough to warrant its own class and tests.
- You need cancellation support.

Use a **lambda job** when:
- The task is trivial and self-contained.
- You want a quick inline job without creating a new class.
- No external services are needed.

---

> **Note:** The performance difference between lambda and concrete jobs has not been formally
> benchmarked. If you are running high-frequency jobs and observe latency, prefer concrete jobs
> and profile to confirm.