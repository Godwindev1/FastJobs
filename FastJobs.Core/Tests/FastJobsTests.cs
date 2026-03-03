using System.Data;

namespace FastJobs;

public class FastJobRepoTests
{
    JobRepoSitoryTest jobTest;
    QueueRepositoryTest QueueTest;

    public FastJobRepoTests(IJobRepository jobrepo, IQueueRepository queueRepository)
    {
        jobTest = new JobRepoSitoryTest(jobrepo);
        QueueTest = new QueueRepositoryTest(queueRepository);
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
            ScheduledAt = DateTime.Now
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
        

    }
}