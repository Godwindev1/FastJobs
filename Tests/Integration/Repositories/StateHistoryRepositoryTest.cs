using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public class StateHistoryRepositoryTest
{
    private readonly FastJobsHostFixture _fixture;
    private readonly IStateHistoryRepository _repository;
    private readonly IJobRepository _jobRepository;

    public StateHistoryRepositoryTest(FastJobsHostFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IStateHistoryRepository>();
        _jobRepository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task StateHistoryRepository_InsertAndSoftDelete()
    {
        var jobId = await InsertJobAsync();
        var state = new State
        {
            JobID = jobId,
            StateName = QueueStateTypes.Enqueued,
            Reason = "created",
            data = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var insertedId = await _repository.InsertAsync(state);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var fetched = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(fetched);
        Assert.Equal(jobId, fetched!.JobID);

        var deletedRows = await _repository.SoftDeleteByIdAsync(insertedId);
        Assert.Equal(1, deletedRows);

        var afterDelete = await _repository.GetByIdAsync(insertedId);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task StateHistoryRepository_InsertBatchAndGetTimestamps()
    {
        var jobId = await InsertJobAsync();
        var states = new[]
        {
            new State { JobID = jobId, StateName = QueueStateTypes.Enqueued, Reason = "created", data = "{}", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new State { JobID = jobId, StateName = QueueStateTypes.Processing, Reason = "started", data = "{}", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new State { JobID = jobId, StateName = QueueStateTypes.Completed, Reason = "done", data = "{}", CreatedAt = DateTime.UtcNow }
        };

        await _repository.InsertAsync(states);

        var timestamps = await _repository.GetTimestampsByJobIdAsync(jobId);
        Assert.NotNull(timestamps);
        Assert.True(timestamps!.EnqueuedAt is not null || timestamps.StartedAt is not null || timestamps.CompletedAt is not null);
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
}
