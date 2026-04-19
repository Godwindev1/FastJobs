using System.Data;
using System.Runtime.CompilerServices;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

public class FastJobRepoTests
{
    JobRepoSitoryTest jobTest;
    QueueRepositoryTest QueueTest;
    ScheduledJobRepositoryTest ScheduledJobTest;
    DBResourceLockingTest ResourceLockingTest;

    public FastJobRepoTests(IJobRepository jobrepo, IQueueRepository queueRepository, IScheduledJobRepository scheduledJobRepository, IServiceProvider provider)
    {
        jobTest = new JobRepoSitoryTest(jobrepo);
        QueueTest = new QueueRepositoryTest(queueRepository);
        ScheduledJobTest = new ScheduledJobRepositoryTest(scheduledJobRepository);
        ResourceLockingTest = new DBResourceLockingTest(provider.GetRequiredService<LockProvider>());
    }

    async public Task RunTest()
    {

        //JOBS TEST
         var job = new Job
        {
            TypeName = "MyApp.Services.EmailService",
            MethodName = "SendWelcomeEmail",

            ParameterTypeNamesJson = "[\"System.String\",\"System.Int32\"]",
            ArgumentsJson = "[\"user@example.com\",123]",

            Queue = "default",
            StateName = "Enqueued",

            RetryCount = 0,
            MaxRetries = 3,

            CreatedAt = DateTime.UtcNow,
        };

         var jobRes = await jobTest.InsertAsync(job);
        jobTest.TestResults.Add(new Tuple<bool, string> ( jobRes != null,  nameof(jobTest.InsertAsync) ) );
        jobTest.TestResults.Add(new Tuple<bool, string> ( await jobTest.GetByIdAsync(jobRes.Id), nameof(jobTest.GetByIdAsync)) );
        jobTest.TestResults.Add(new Tuple<bool, string> ( await jobTest.UpdateRecord(jobRes.Id,  "Queue = @Queue", new Job { Queue = "Error Queue"}  ), nameof(jobTest.UpdateRecord)) );
        
        //QUEUE ENTRY TEST
        var queue = new Queue
        {
            QueueName = "Default",
            JobId = jobRes.Id,
            Priority = 1,
            DequeuedAt = DateTime.Now
        };

        //insert
        var Entry = await QueueTest.Enqueue(queue);
        QueueTest.TestResults.Add(new Tuple<bool, string> ( Entry != null,  nameof(QueueTest.Enqueue) ) );
        QueueTest.TestResults.Add(new Tuple<bool, string> ( await QueueTest.GetQueueEntry(Entry.Id), nameof(QueueTest.GetQueueEntry)) );
        QueueTest.TestResults.Add(new Tuple<bool, string > ( await QueueTest.Dequeue("Default") ?? false, nameof(QueueTest.Dequeue) ));
        QueueTest.TestResults.Add(new Tuple<bool, string> ( await QueueTest.RemoveAsync(Entry.Id), nameof(QueueTest.RemoveAsync)) );


        foreach(var result in QueueTest.TestResults)
        {
            Console.WriteLine($"Test Result for {result.Item2} = { result.Item1 }");
        }

        
        jobTest.TestResults.Add(new Tuple<bool, string> ( await jobTest.DeleteRecord(jobRes.Id), "DeleteAsync") );


        foreach(var result in jobTest.TestResults)
        {
            Console.WriteLine($"Test Result for {result.Item2} = { result.Item1 }");
        }
        

        //SCHEDULED JOB TEST
        var scheduledJob = new ScheduledJobInfo
        {
            JobId = jobRes.Id,
            ScheduledTo = DateTime.UtcNow.AddHours(2)
        };

        var InsertedScheduledJob = await ScheduledJobTest.InsertAsync(scheduledJob);
        ScheduledJobTest.TestResults.Add(new Tuple<bool, string>(InsertedScheduledJob != null, nameof(ScheduledJobTest.InsertAsync)));
        ScheduledJobTest.TestResults.Add(new Tuple<bool, string>(await ScheduledJobTest.GetByIdAsync(InsertedScheduledJob.Id), nameof(ScheduledJobTest.GetByIdAsync)));
        
        var updatedScheduledJob = new ScheduledJobInfo
        {
            Id = InsertedScheduledJob.Id,
            JobId = jobRes.Id,
            ScheduledTo = DateTime.UtcNow.AddHours(3)
        };
        ScheduledJobTest.TestResults.Add(new Tuple<bool, string>(await ScheduledJobTest.UpdateRecord(InsertedScheduledJob.Id, updatedScheduledJob), nameof(ScheduledJobTest.UpdateRecord)));
        ScheduledJobTest.TestResults.Add(new Tuple<bool, string>(await ScheduledJobTest.GetReadyJobs(), nameof(ScheduledJobTest.GetReadyJobs)));
        ScheduledJobTest.TestResults.Add(new Tuple<bool, string>(await ScheduledJobTest.DeleteRecord(InsertedScheduledJob.Id), nameof(ScheduledJobTest.DeleteRecord)));

        foreach(var result in ScheduledJobTest.TestResults)
        {
            Console.WriteLine($"Test Result for {result.Item2} = { result.Item1 }");
        }

        //RESOURCE LOCKING
        ResourceLockingTest.TestResults.Add(
            new Tuple<bool, string> ( 
                await ResourceLockingTest.AcquireLock($"FastJobs.{job.MethodName}", TimeSpan.FromSeconds(20)),
                nameof(ResourceLockingTest.AcquireLock)
            )
        );

        ResourceLockingTest.ReleaseLock();

        
        foreach(var result in ResourceLockingTest.TestResults)
        {
            Console.WriteLine($"Test Result for {result.Item2} = { result.Item1 }");
        }

    }
}