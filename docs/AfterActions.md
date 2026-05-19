---
title: After Actions
nav_order: 9
---

# After Actions

After Actions are callbacks that execute automatically once a job completes. They allow you to
define what happens next — whether that is re-enqueueing the job, deleting it, triggering a
separate job, or running any custom logic — without coupling that behaviour to the job itself.

---

## How It Works

An After Action is a class that implements `IAfterAction`. When a job finishes, FastJobs checks
whether an After Action was registered for it and executes `ExecuteAsync` before the job is
considered fully settled.

```csharp
public interface IAfterAction
{
    Task ExecuteAsync(CancellationToken token);
}
```

After Actions receive the completed job's context via `IJobContext`, which is injected automatically
by FastJobs. This gives the action access to the job's ID, retry count, queue, and other metadata
without you having to pass anything manually.

---

## Registering an After Action

Use `.AddAfterAction()` in the fluent chain before calling `.Start()`:

```csharp
await FastJobServer.EnqueueJob<ComplexTestJob>()
    .AddAfterAction(builder => builder.WithType<MyAfterAction>())
    .Start();
```

This works with all job types — enqueued, delayed, and recurring.

---

## Built-in After Actions

FastJobs ships with two built-in After Action types for common post-job behaviours.

### EnqueueAfterAction

Re-enqueues the completed job as a fresh job. The job is reset — its ID, retry count, and any
previous After Action reference are cleared before it is placed back in the queue.

```csharp
await FastJobServer.EnqueueJob<ComplexTestJob>()
    .AddAfterAction(builder => builder.WithType<EnqueueAfterAction>())
    .Start();
```

**Use this when** you want a job to repeat indefinitely after each completion, similar to a
recurring job but triggered by completion rather than a schedule.

---

### DeleteAfterAction

Deletes the job record from the store once execution completes. The job leaves no trace in the
system after it finishes.

```csharp
await FastJobServer.EnqueueJob<ComplexTestJob>()
    .AddAfterAction(builder => builder.WithType<DeleteAfterAction>())
    .Start();
```

**Use this when** the job is a one-off operation and you want to keep your job store clean without
relying on manual cleanup or expiry dates.

---

## Building a Custom After Action

Implement `IAfterAction` and inject `IJobContext` and `IServiceScopeFactory` in the constructor.
`IJobContext` gives you access to the completed job; `IServiceScopeFactory` lets you resolve
scoped services safely from within the action.

```csharp
using FastJobs;

namespace YourApp.AfterActions;

public class NotifyAfterAction : IAfterAction
{
    private readonly IJobContext _jobContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public NotifyAfterAction(IJobContext jobContext, IServiceScopeFactory scopeFactory)
    {
        _jobContext = jobContext;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notifier.SendAsync($"Job {_jobContext.CurrentJob.Id} completed.", token);
    }
}
```

Then register it like any other After Action:

```csharp
await FastJobServer.EnqueueJob<ReportJob>()
    .AddAfterAction(builder => builder.WithType<NotifyAfterAction>())
    .Start();
```

---

## Chaining with Other Fluent Options

After Actions compose with the rest of the fluent API. The chain order does not matter as long as
`.Start()` is called last.

```csharp
await FastJobServer.EnqueueJob<ComplexTestJob>()
    .SetPriority(JobPriority.High)
    .SetMaxRetryCount(3)
    .AddAfterAction(builder => builder.WithType<DeleteAfterAction>())
    .Start();
```

---

## Notes

- An After Action runs once per job completion, not per retry attempt. If a job fails and retries,
  the After Action is not triggered until the job either succeeds or exhausts its retry limit.
- After Actions are executed within the FastJobs engine context. Avoid long-running blocking
  operations inside `ExecuteAsync` — use async patterns throughout.
- Only one After Action can be registered per job. Registering a second call to `.AddAfterAction()`
  on the same chain will override the first.