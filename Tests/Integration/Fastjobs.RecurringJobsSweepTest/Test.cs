using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }

//TESTS GET ORPHANED JOBS SHOULD ONLY RETURN JOBS THAT ARE NOT IN EXPIRED STATE BUT NOT SCHEDULED
[Collection("FastJobServerTests")]
public class RecurringJobSweepTest : IClassFixture<RecurringJobSweepJobTestFixture>
{
    private readonly RecurringJobSweepJobTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IJobRepository _repo;
    private readonly IRecurringJobRepository RecurringRepo;
    private readonly IQueueRepository QueueRepo;
    private readonly IScheduledJobRepository scheduledJobRepository;

    public RecurringJobSweepTest(RecurringJobSweepJobTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _repo = fixture.Host.Services.GetService<IJobRepository>();
        RecurringRepo = fixture.Host.Services.GetService<IRecurringJobRepository>();
        QueueRepo = fixture.Host.Services.GetService<IQueueRepository>();
        scheduledJobRepository = fixture.Host.Services.GetService<IScheduledJobRepository>();
        _output = output;
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            
            if ( await condition())
                return;

            await Task.Delay(100); // poll interval
        }
    }

    [Fact]
    public async Task Job_should_be_scheduled_by_recovery_sweep()
    {
        //in this test we will inject a job into the system without scheduling it (so no recurring logic). and then wait for the recovery sweep to Start the Scehduling chain 
        //Criteria For A Job To be caught by recovery sweep (Job has Not Expired, it Does not have A Next Schedule or NextScheduledID, and it has passed it ScheduledTime to run (called NextScheduledTime in repo))
        //PLAN: use only one Worker. Schedule A recurring job Directly through the Repositories So The Fastjobs Engine is Not Notified of a New Recurring Job and Does not schedule IT. Then Wait for the 
        //recovery sweep (So only when Recovery Sweep is Done should An entry appear in Scheduled Jobs Repo)

        // Arrange
        var now = DateTime.UtcNow;

        Job okJ =  new Job
        {
            AfterActionId = null,
            JobType = JobTypes.Recurring,
            TypeName = typeof(RecurringJobSweepTestJob).ToString(),
            MethodName = "ExecuteAsync",
            MethodDeclaringTypeName = typeof(RecurringJobSweepTestJob).ToString(),
            ParameterTypeNamesJson = "[]",
            ArgumentsJson = "[]",
            Queue = "Default",
            stateID = 0,
            StateName = QueueStateTypes.Scheduled,
            RetryCount = 0,
            MaxRetries = 3,
            Priority = 0,
            misfirePolicy = (int)MisfirePolicy.FireOnce,
            CreatedAt = now,
            ScheduledRunAt = now,
            ExpiresAt = null
        };

        var id = await _repo.InsertAsync(okJ);

        RecurringJob rkj = new RecurringJob
        {
            JobId = id,
            NextScheduledID = null,
            CronExpression = null,
            StartTime = now,
            IntervalTicks = TimeSpan.FromMinutes(1).Ticks,
            NextScheduledTime = now,
            IsConcurrent = true,
            IsCron = false,
            ExecutingInstances = 0,
            ExecutedInstances = 0
        };

        Queue queue = new Queue {
            Priority = 1,
            JobId = id,
            QueueName = QueueNames.Critical
        };

        
        //await QueueRepo.EnqueueAsync(queue); enqueuing the job would make it get scheduled 
        await RecurringRepo.InsertAsync(rkj);

        // Act
        await WaitUntilAsync(
            condition: async () => (await scheduledJobRepository.GetAllAsync()).Count >= 1,
            timeout: TimeSpan.FromSeconds(120));

        var allEntries = await scheduledJobRepository.GetAllAsync();

        foreach (var e in allEntries)
        {
            _output.WriteLine($"id: {e.JobId} Scheduled too: {e.ScheduledTo}, RecurringJob: {(await RecurringRepo.GetAllAsync()).FirstOrDefault().NextScheduledID}");
        }

        // Assert
        var All = await scheduledJobRepository.GetAllAsync();
        var count = All.Count;
       ;
        Assert.Equal(true, count > 0);
    }
}