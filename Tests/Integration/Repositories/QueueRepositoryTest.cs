using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public abstract class QueueRepositoryTest<TFixture> where TFixture : FastJobsHostFixtureBase  
{
    private readonly TFixture _fixture;
    private readonly IQueueRepository _repository;
    private readonly IJobRepository _jobRepository;

    public QueueRepositoryTest(TFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IQueueRepository>();
        _jobRepository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task QueueRepository_EnqueueAndGetByJob()
    {
        var jobId = await InsertJobAsync();
        var queueEntry = CreateQueueEntry(jobId, $"Queue-{Guid.NewGuid():N}");

        var insertedId = await _repository.EnqueueAsync(queueEntry);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var fetched = await _repository.GetQueueEntry(insertedId);
        var byJob = await _repository.GetByJob(jobId);

        Assert.NotNull(fetched);
        Assert.NotNull(byJob);
        Assert.Equal(jobId, fetched!.JobId);
        Assert.Equal(queueEntry.QueueName, fetched.QueueName);
        Assert.Equal(insertedId, byJob!.Id);
    }

    [Fact]
    public async Task QueueRepository_UpdateAndRemove()
    {
        var jobId = await InsertJobAsync();
        var queueEntry = CreateQueueEntry(jobId, $"UpdateQueue-{Guid.NewGuid():N}");
        var insertedId = await _repository.EnqueueAsync(queueEntry);

        var existing = await _repository.GetQueueEntry(insertedId);
        Assert.NotNull(existing);

        existing!.QueueName = "UpdatedQueue";
        existing.Priority = 7;
        existing.isDequeued = true;
        existing.DequeuedAt = DateTime.UtcNow;

        var updatedRows = await _repository.Update(existing);
        Assert.Equal(1, updatedRows);

        var updated = await _repository.GetQueueEntry(insertedId);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedQueue", updated!.QueueName);
        Assert.True(updated.isDequeued);
        Assert.Equal(7, updated.Priority);

        var removed = await _repository.RemoveAsync(insertedId);
        Assert.True(removed);
        Assert.Null(await _repository.GetQueueEntry(insertedId));
    }

    [Fact]
    public async Task QueueRepository_GetAllExistsAndDequeue()
    {
        var jobId = await InsertJobAsync();
        var first = CreateQueueEntry(jobId, $"ListQueue-{Guid.NewGuid():N}");
        var second = CreateQueueEntry(jobId, $"DequeueQueue-{Guid.NewGuid():N}");

        var firstId = await _repository.EnqueueAsync(first);
        var secondId = await _repository.EnqueueAsync(second);

        var allEntries = await _repository.GetAllQueueEntries();
        Assert.Contains(allEntries, entry => entry.Id == firstId);
        Assert.Contains(allEntries, entry => entry.Id == secondId);

        var exists = await _repository.ExistsAny();
        Assert.True(exists);

        var dequeued = await _repository.Dequeue(second.QueueName);
        var dequeuedEntry = await _repository.GetQueueEntry(secondId);

        Assert.NotNull(dequeuedEntry);
        Assert.True(dequeuedEntry is null || dequeuedEntry.JobId == second.JobId || dequeued is null || dequeued.JobId == second.JobId);
    }

    private async Task<long> InsertJobAsync()
    {
        var job = CreateJob();
        return await _jobRepository.InsertAsync(job);
    }

    private static Job CreateJob()
    {
        return new Job
        {
            AfterActionId = null,
            JobType = "Enqueued",
            TypeName = "LibraryTest.FailTestJob",
            MethodName = "FailTestJob",
            MethodDeclaringTypeName = "LibraryTest.FailTestJob",
            ParameterTypeNamesJson = "[]",
            ArgumentsJson = "[]",
            Queue = "Default",
            stateID = 0,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = 3,
            Priority = 0,
            misfirePolicy = (int)MisfirePolicy.FireOnce,
            CreatedAt = DateTime.UtcNow,
            ScheduledRunAt = DateTime.UtcNow.AddMinutes(5),
            ExpiresAt = null
        };
    }

    private static Queue CreateQueueEntry(long jobId, string queueName)
    {
        return new Queue
        {
            QueueName = queueName,
            JobId = jobId,
            Priority = 1,
            isDequeued = false,
            IsMisfireRecovery = false,
            DequeuedAt = DateTime.UtcNow
        };
    }
}

[Collection("MSSQLHostFixture_Collection")]
[Trait("Provider", "MSSQL")]
public class MsSql_Queue_repositoryTest : QueueRepositoryTest<MsSqlFastJobsHostFixture>
{
    public MsSql_Queue_repositoryTest(MsSqlFastJobsHostFixture fixture) : base(fixture) { }
}

[Collection("MariaDBHostFixture_Collection")]
[Trait("Provider", "MariaDB")]
public class MariaDB_Queue_repositoryTest : QueueRepositoryTest<MariaDbFastJobsHostFixture>
{
    public MariaDB_Queue_repositoryTest(MariaDbFastJobsHostFixture fixture) : base(fixture) { }
}