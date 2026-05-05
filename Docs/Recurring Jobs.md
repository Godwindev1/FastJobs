# Recurring Jobs

Recurring Jobs Allow You To Use Either Simple `Timespan based Intervals` or `Cron Expressions`  To Configure Jobs That Repeats After Configured Periods of Time. 

E.g *A Recurring Job To Send Email Remainders To Team Members Who Are behind Schedule on thier Task Every Monday At 11:00AM*

**To Configure A Recurring Job you Make A Call To The `AddRecurringJob.ScheduleJob`**

```csharp
    await FastJobServer.AddRecurringJob(() => Console.WriteLine("Testing Recurring Jobs in FastJobs"))
    .WithInterval(TimeSpan.FromMinutes(40), DateTime.Now)// Simple Interval Based to Run every 40 minutes, starting immediately
    .Start();
```
It Can Also Be Used With Concrete Jobs just Like Scheduled and Enqueued Jobs can


## Configuring Parameters Using Fluent Patterns 
Recurring Jobs add a Few More Methods to It Fluent Builder and Reuses Most Of Others from Other job Types 

*interval Based Job To Repeat Every 45 seconds*
```csharp
    await FastJobServer.
    AddRecurringJob(() => Console.WriteLine("Hello World" ))
    .WithInterval(TimeSpan.FromSeconds(45))
    .Start();
```

*Cron expression Based Job *
```csharp
    await FastJobServer.
    AddRecurringJob(() => Console.WriteLine("Hello World" ))
    .AddCronExpression("*/1 * * * *") // Every minute
    .Start();
```

*For Recurring Job the RunAt Method Specifys The Absolut UTC time when The First Run of These Job should Happen*
```csharp 
    //Set FirstRunTime to be After 45 seconds
    await FastJobServer.
    AddRecurringJob(() => Console.WriteLine("Hello World" ))
    .RunAt(DateTime.Now.AddSeconds(45))
    .Start();
```

*An Alternative to RunAt for Delaying FirstRun*
```csharp 
    //Set FirstRunTime to be After 4 seconds
    await FastJobServer.
    AddRecurringJob(() => Console.WriteLine("Hello World" ))
    .WaitDelay(TimeSpan.FromSeconds(4))
    .Start();
```

*Allows a New instances of the Job to Start If a Previous one is already running*
```csharp
    await FastJobServer.
    AddRecurringJob(() => Console.WriteLine("Hello World" ))
    .AllowConcurrentExecution()
    .Start();

```


**Guideline While using Recurring Jobs**
- You Can Only use One Of `WithInterval()` or `AddCronExpression()`. 
- Similarly You Can Only use One Of `RunAt()` or `WaitDelay()`

---
new Methods Can Be Added Anywhere in the Chain Along With Other Fluent Methods As long as the Above Guidelines are Adhered To .
```csharp
await FastJobServer.EnqueueJob<TestJob>()
.RunAt(DateTime.Now.AddMinutes(45))
.SetPriority(JobPriority.High)
.SetMaxRetryCount(3)
.SetExpiresAt(DateTime.Now.AddMinutes(10))
.Start();
```