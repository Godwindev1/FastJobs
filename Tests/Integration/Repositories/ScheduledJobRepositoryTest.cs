using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public class ScheduledJobRepositoryTest
{
    private readonly FastJobsHostFixture _fixture;
    private readonly IScheduledJobRepository _repository;
    private readonly IJobRepository _jobRepository;

    public ScheduledJobRepositoryTest(FastJobsHostFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IScheduledJobRepository>();
        _jobRepository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task ScheduledJobRepository_InsertGetUpdateDelete()
    {
        var jobId = await InsertJobAsync();
        var scheduledJob = new ScheduledJobInfo
        {
            JobId = jobId,
            ScheduledTo = DateTime.UtcNow.AddMinutes(-1)
        };

        var insertedId = await _repository.InsertAsync(scheduledJob);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var byId = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(byId);
        Assert.Equal(jobId, byId!.JobId);

        var readyJobs = await _repository.GetReadyJobsAsync();
        Assert.Contains(readyJobs, item => item.Id == insertedId);

        byId.ScheduledTo = DateTime.UtcNow.AddMinutes(10);
        var updatedRows = await _repository.UpdateByIdAsync(byId);
        Assert.Equal(1, updatedRows);

        var updated = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(updated);
        Assert.True(updated!.ScheduledTo > DateTime.UtcNow);

        var deletedRows = await _repository.DeleteByIdAsync(insertedId);
        Assert.Equal(1, deletedRows);
        Assert.Null(await _repository.GetByIdAsync(insertedId));
    }

    [Fact]
    public async Task ScheduledJobRepository_GetAllDeleteMultipleAndNextScheduled()
    {
        var jobId = await InsertJobAsync();
        var pastJob = new ScheduledJobInfo { JobId = jobId, ScheduledTo = DateTime.UtcNow.AddMinutes(-1) };
        var futureJob = new ScheduledJobInfo { JobId = jobId, ScheduledTo = DateTime.UtcNow.AddMinutes(5) };

        var pastId = await _repository.InsertAsync(pastJob);
        var futureId = await _repository.InsertAsync(futureJob);

        var all = await _repository.GetAllAsync();
        Assert.Contains(all, item => item.Id == pastId);
        Assert.Contains(all, item => item.Id == futureId);

        var next = await _repository.GetNextScheduledJob();
        Assert.True(next is null || next.Id > 0);

        var deletedRows = await _repository.DeleteMultipleAsync(new[] { pastId, futureId });
        Assert.True(deletedRows >= 0);
        Assert.DoesNotContain(await _repository.GetAllAsync(), item => item.Id == pastId || item.Id == futureId);
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
