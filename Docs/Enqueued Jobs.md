# Enqueued Jobs

Enqueued Jobs Get placed Directly in Queue For Execution And Assuming Worker Availability. They Are Instantly Picked Up For Execution 

**To Enqueue Jobs you Make A Call To The `FastjobServer.EnqueueJob` Service**

```csharp
    await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) 
    .Start();
```

**To Enqueue A Concrete Job use This Pattern**

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


await FastJobServer.EnqueueJob<TestJob>().Start();
```

## Configuring Parameters Using Fluent Patterns 
You Can Also Configure Important Paramters Concerning Job Execution, Retries and Expiry Using The Fluent Building Pattern Like So Below 

**Set Job Priority By**
```csharp
    await FastJobServer.
    EnqueueJob(() => Console.WriteLine("Hello World" ))
    .SetPriority(JobPriority.High)
    .Start();
```
**Set Max Retry Count**
```csharp 
    await FastJobServer.
    EnqueueJob(() => Console.WriteLine("Hello World" ))
    .SetMaxRetryCount(3)
    .Start();
```

**Set Expiry Date**
```csharp
await FastJobServer.
    EnqueueJob(() => Console.WriteLine("Hello World" ))
    .SetExpiresAt(DateTime.Now.AddMinutes(10))
    .Start();
```

---


All Fluent Patterns Are Work When using Concrete Jobs As Well And Can Be Chained One After the Other. However a Call to .Start() Must be used to end it.

```csharp
await FastJobServer.EnqueueJob<TestJob>()
.SetPriority(JobPriority.High)
.SetMaxRetryCount(3)
.SetExpiresAt(DateTime.Now.AddMinutes(10))
.Start();
```