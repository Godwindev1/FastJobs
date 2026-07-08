using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public class RecurringJobRepositoryTest
{
    private readonly FastJobsHostFixture _fixture;
    private readonly IRecurringJobRepository _repository;
    private readonly IJobRepository _jobRepository;

    public RecurringJobRepositoryTest(FastJobsHostFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IRecurringJobRepository>();
        _jobRepository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task RecurringJobRepository_InsertGetUpdateDelete()
    {
        var jobId = await InsertJobAsync();
        var job = await _jobRepository.GetByIdAsync(jobId);
        Assert.NotNull(job);

        var recurringJob = CreateRecurringJob(jobId);
        var insertedId = await _repository.InsertAsync(recurringJob);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var byId = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(byId);
        Assert.Equal(jobId, byId!.JobId);

        var byJob = await _repository.GetByJob(job!);
        Assert.NotNull(byJob);
        Assert.Equal(insertedId, byJob!.id);

        byId.CronExpression = "0 * * * *";
        byId.NextScheduledTime = DateTime.UtcNow.AddHours(1);
        byId.ExecutedInstances = 2;

        var updatedRows = await _repository.UpdateByIdAsync(byId);
        Assert.Equal(1, updatedRows);

        var updated = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(updated);
        Assert.Equal("0 * * * *", updated!.CronExpression);
        Assert.Equal(2, updated.ExecutedInstances);

        var deletedRows = await _repository.DeleteByIdAsync(insertedId);
        Assert.Equal(1, deletedRows);
        Assert.Null(await _repository.GetByIdAsync(insertedId));
    }

    [Fact]
    public async Task RecurringJobRepository_GetAllAndQueryHelpers()
    {
        var jobId = await InsertJobAsync();
        var dueRecurring = CreateRecurringJob(jobId, DateTime.UtcNow.AddMinutes(-2));
        var futureRecurring = CreateRecurringJob(jobId, DateTime.UtcNow.AddMinutes(10), cronExpression: "0 0 * * *");

        var dueId = await _repository.InsertAsync(dueRecurring);
        var futureId = await _repository.InsertAsync(futureRecurring);

        var all = await _repository.GetAllAsync();
        Assert.Contains(all, item => item.id == dueId);
        Assert.Contains(all, item => item.id == futureId);

        var orphaned = await _repository.GetOrphanedRecurringJobsAsync();
        Assert.Contains(orphaned, item => item.id == dueId);

        var due = await _repository.GetDueAsync();
        Assert.Contains(due, item => item.id == dueId);

        var next = await _repository.GetNextScheduledRecurringJob();
        Assert.True(next is null || next.id > 0);
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

    private static RecurringJob CreateRecurringJob(long jobId, DateTime? nextScheduledTime = null, string? cronExpression = null)
    {
        var now = DateTime.UtcNow;
        return new RecurringJob
        {
            JobId = jobId,
            NextScheduledID = null,
            CronExpression = cronExpression ?? "*/5 * * * *",
            StartTime = now,
            IntervalTicks = null,
            NextScheduledTime = nextScheduledTime ?? now.AddMinutes(5),
            IsConcurrent = true,
            IsCron = true,
            ExecutingInstances = 0,
            ExecutedInstances = 0
        };
    }
}
