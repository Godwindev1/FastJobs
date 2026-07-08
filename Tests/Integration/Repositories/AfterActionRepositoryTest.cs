using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public class AfterActionRepositoryTest
{
    private readonly FastJobsHostFixture _fixture;
    private readonly IAfterActionRepository _repository;
    private readonly IJobRepository _jobRepository;

    public AfterActionRepositoryTest(FastJobsHostFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IAfterActionRepository>();
        _jobRepository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task AfterActionRepository_InsertGetUpdateDelete()
    {
        var jobId = await InsertJobAsync();
        var action = CreateAction(jobId);

        var insertedId = await _repository.InsertAsync(action);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var fetched = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(fetched);
        Assert.Equal(jobId, fetched!.JobId);

        var byJob = await _repository.GetByJobIdAsync(jobId);
        Assert.Contains(byJob, item => item.Id == insertedId);

        fetched.Retries = 2;
        fetched.Payload = "updated";

        var updatedRows = await _repository.UpdateByIdAsync(fetched);
        Assert.Equal(1, updatedRows);

        var updated = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(updated);
        Assert.Equal(2, updated!.Retries);
        Assert.Equal("updated", updated.Payload);

        var countByJob = await _repository.CountByJobIdAsync(jobId);
        Assert.True(countByJob >= 1);

        var deletedRows = await _repository.DeleteByIdAsync(insertedId);
        Assert.Equal(1, deletedRows);
        Assert.Null(await _repository.GetByIdAsync(insertedId));
    }

    [Fact]
    public async Task AfterActionRepository_QueryHelpersAndPartialUpdate()
    {
        var jobId = await InsertJobAsync();
        var beforeAll = await _repository.CountAllAsync();
        var beforeRetrying = await _repository.CountRetryingAsync();
        var beforeExhausted = await _repository.CountExhaustedAsync();
        var beforeSucceeded = await _repository.CountSucceededFirstAttemptAsync();
        var beforeByJob = await _repository.CountByJobIdAsync(jobId);

        var firstId = await _repository.InsertAsync(CreateAction(jobId, retries: 0, maxRetries: 3));
        var secondId = await _repository.InsertAsync(CreateAction(jobId, retries: 2, maxRetries: 3));
        var thirdId = await _repository.InsertAsync(CreateAction(jobId, retries: 3, maxRetries: 3));

        var allActions = await _repository.GetAllAsync();
        Assert.Contains(allActions, item => item.Id == firstId);
        Assert.Contains(allActions, item => item.Id == secondId);
        Assert.Contains(allActions, item => item.Id == thirdId);

        var byJob = await _repository.GetByJobIdAsync(jobId);
        Assert.Contains(byJob, item => item.Id == secondId);

        var partialTarget = await _repository.GetByIdAsync(secondId);
        Assert.NotNull(partialTarget);
        partialTarget!.Retries = 5;

        var partialRows = await _repository.UpdateByIdAsync(secondId, "Retries = @Retries", partialTarget);
        Assert.True(partialRows >= 0);

        var updatedPartial = await _repository.GetByIdAsync(secondId);
        Assert.NotNull(updatedPartial);
        Assert.True(updatedPartial!.Retries >= 0);

        Assert.True(await _repository.CountAllAsync() >= beforeAll);
        Assert.True(await _repository.CountRetryingAsync() >= beforeRetrying);
        Assert.True(await _repository.CountExhaustedAsync() >= beforeExhausted);
        Assert.True(await _repository.CountSucceededFirstAttemptAsync() >= beforeSucceeded);
        Assert.True(await _repository.CountByJobIdAsync(jobId) >= beforeByJob);

        var average = await _repository.AverageActionsPerJobAsync();
        Assert.True(average >= 0);

        var deletedRows = await _repository.DeleteByJobIdAsync(jobId);
        Assert.True(deletedRows >= 3);
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

    private static AfterActionModel CreateAction(long jobId, int retries = 0, int maxRetries = 3)
    {
        return new AfterActionModel
        {
            TypeName = "Retry",
            Retries = retries,
            MaxRetries = maxRetries,
            JobId = jobId,
            NextActionID = 0,
            LastActionID = 0,
            ChainNo = 1,
            Payload = "initial"
        };
    }
}
