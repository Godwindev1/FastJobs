# Scheduled / Delayed Jobs

Scheduled jobs Allow You to Set A Delay With Timespan or Set the Date on Which this Job Would Run. 
When its Due the Scheduler Enqueues The Job in A `Critical Queue`  for immediate Execution  

**To Schedule Jobs you Make A Call To The `FastjobServer.ScheduleJob`**

```csharp
    await FastJobServer.ScheduleJob(() => Console.WriteLine("Hello Kaboom At " + DateTime.Now))
    .WaitDelay(TimeSpan.FromSeconds(45)) 
    .Start();
```

**To Schedule A Concrete Job use This Pattern**

```csharp
public class TestJob : IBackGroundJob
{
    private readonly ILogger<TestJob> _logger;

    public TestJob(ILogger<TestJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("started at {Time}", DateTime.UtcNow);
        // Simulate work
        await Task.Delay(5000);
        _logger.LogInformation("completed at {Time}", DateTime.UtcNow);
    }
}


await FastJobServer.ScheduleJob<TestJob>()
.WaitDelay(TimeSpan.FromSeconds(45))
.Start();

```

## Configuring Parameters Using Fluent Patterns 
Scheduled Jobs Add Two Extra Fluent Methods To Those Used By EnqueuedJobs

**To Schedule With a Delay**
```csharp
    await FastJobServer.
    ScheduleJob(() => Console.WriteLine("Hello World" ))
    .WaitDelay(TimeSpan.FromSeconds(45))
    .Start();
```
**To Schedule With A Datetime**
```csharp 
    await FastJobServer.
    ScheduleJob(() => Console.WriteLine("Hello World" ))
    .RunAt(DateTime.Now.AddSeconds(45))
    .Start();
```

**NOTE**
You Can Only Use One of these Scheduling methods in the Fluent Chain as They Override Eachother So which ever is latest in the Chain Persists with the Job

---
The New Methods Can Be Added Anywhere in the Chain aswell
```csharp
await FastJobServer.EnqueueJob<TestJob>()
.RunAt(DateTime.Now.AddMinutes(45))
.SetPriority(JobPriority.High)
.SetMaxRetryCount(3)
.SetExpiresAt(DateTime.Now.AddMinutes(10))
.Start();
```