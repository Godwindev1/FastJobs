
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

public abstract class JobRepositoryTest<TFixture> where TFixture : FastJobsHostFixtureBase  
{
    protected readonly TFixture _fixture;
    private readonly IJobRepository _repository;

    public JobRepositoryTest(TFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task JobRepository_Insert()
    {
        var job = CreateJob();

        var resultId = await _repository.InsertAsync(job);
        Assert.True(resultId > 0, "Expected a positive auto-generated ID");
    }

    [Fact]
    public async Task JobRepository_GetByID()
    {
        var job = CreateJob();

        var resultId = await _repository.InsertAsync(job);
        Assert.True(resultId > 0, "Insert failed; expected a positive auto-generated ID");

        var resultJob = await _repository.GetByIdAsync(resultId);
        Assert.NotNull(resultJob);
        Assert.Equal(resultId, resultJob!.Id);
    }

    [Fact]
    public async Task JobRepository_GetAllAsync()
    {
        var job = CreateJob(stateName: $"GetAll-{Guid.NewGuid():N}");
        var insertedId = await _repository.InsertAsync(job);

        var jobs = await _repository.GetAllAsync();
        Assert.NotNull(jobs);
        Assert.Contains(jobs!, item => item.Id == insertedId);
    }

    [Fact]
    public async Task JobRepository_DeleteByIdAsync()
    {
        var job = CreateJob(stateName: $"Delete-{Guid.NewGuid():N}");
        var insertedId = await _repository.InsertAsync(job);

        var affectedRows = await _repository.DeleteByIdAsync(insertedId);
        Assert.Equal(1, affectedRows);

        var deletedJob = await _repository.GetByIdAsync(insertedId);
        Assert.Null(deletedJob);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task JobRepository_UpdateByIdAsync_Two_overloads(bool isPartialUpdate)
    {
        var job = CreateJob(stateName: $"Update-{Guid.NewGuid():N}", retryCount: 1);
        var insertedId = await _repository.InsertAsync(job);
        Assert.True(insertedId > 0);

        job.Id = insertedId;
        job.StateName = "UpdatedState";
        job.RetryCount = 7;

        if (isPartialUpdate)
        {
            var affectedRows = await _repository.UpdateByIdAsync(insertedId, "StateName = @StateName, RetryCount = @RetryCount", job);
            Assert.Equal(1, affectedRows);
        }
        else
        {
            job.JobType = JobTypes.Recurring;
            job.Queue = "Priority";

            var affectedRows = await _repository.UpdateByIdAsync(job);
            Assert.Equal(1, affectedRows);
        }

        var updatedJob = await _repository.GetByIdAsync(insertedId);
        Assert.NotNull(updatedJob);
        Assert.Equal("UpdatedState", updatedJob!.StateName);
        Assert.Equal(7, updatedJob.RetryCount);

        if (!isPartialUpdate)
        {
            Assert.Equal(JobTypes.Recurring, updatedJob.JobType);
            Assert.Equal("Priority", updatedJob.Queue);
        }
    }

    [Fact]
    public async Task JobRepository_CountByStateAsync()
    {
        var stateName = $"CountState-{Guid.NewGuid():N}";
        var before = await _repository.CountByStateAsync(stateName);

        await _repository.InsertAsync(CreateJob(stateName: stateName));
        await _repository.InsertAsync(CreateJob(stateName: stateName));

        var after = await _repository.CountByStateAsync(stateName);
        Assert.Equal(before + 2, after);
    }

    [Fact]
    public async Task JobRepository_CountRetryingAsync()
    {
        var before = await _repository.CountRetryingAsync();

        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Processing, retryCount: 2));
        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Processing, retryCount: 3));

        var after = await _repository.CountRetryingAsync();
        Assert.Equal(before + 2, after);
    }

    [Fact]
    public async Task JobRepository_CountCompletedSinceAsync()
    {
        var since = DateTime.UtcNow.AddMinutes(-5);
        var before = await _repository.CountCompletedSinceAsync(since);

        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Completed, createdAt: DateTime.UtcNow.AddMinutes(-1)));
        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Completed, createdAt: DateTime.UtcNow.AddMinutes(-10)));

        var after = await _repository.CountCompletedSinceAsync(since);
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public async Task JobRepository_CountFailedSinceAsync()
    {
        var since = DateTime.UtcNow.AddMinutes(-5);
        var before = await _repository.CountFailedSinceAsync(since);

        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Failed, createdAt: DateTime.UtcNow.AddMinutes(-1)));
        await _repository.InsertAsync(CreateJob(stateName: QueueStateTypes.Failed, createdAt: DateTime.UtcNow.AddMinutes(-10)));

        var after = await _repository.CountFailedSinceAsync(since);
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public async Task JobRespository_CountStateBetween()
    {
        var stateName = $"Between-{Guid.NewGuid():N}";
        var from = DateTime.UtcNow.AddMinutes(-10);
        var to = DateTime.UtcNow.AddMinutes(10);
        var before = await _repository.CountStateBetween(stateName, from, to);

        await _repository.InsertAsync(CreateJob(stateName: stateName, createdAt: DateTime.UtcNow.AddMinutes(-2)));
        await _repository.InsertAsync(CreateJob(stateName: stateName, createdAt: DateTime.UtcNow.AddMinutes(-20)));

        var after = await _repository.CountStateBetween(stateName, from, to);
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public async Task JobRepository_GetMisfiredJobsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(5);
        var misfiredJob = CreateJob(stateName: QueueStateTypes.Processing, jobType: "Recurring", scheduledRunAt: cutoff.AddMinutes(-1));
        var notMisfiredJob = CreateJob(stateName: QueueStateTypes.Processing, jobType: "Recurring", scheduledRunAt: cutoff.AddMinutes(1));

        var misfiredId = await _repository.InsertAsync(misfiredJob);
        var notMisfiredId = await _repository.InsertAsync(notMisfiredJob);

        var misfiredJobs = await _repository.GetMisfiredJobsAsync(cutoff);
        Assert.Contains(misfiredJobs, job => job.Id == misfiredId);
        Assert.DoesNotContain(misfiredJobs, job => job.Id == notMisfiredId);
    }

    [Fact]
    public async Task JobRepository_PruneCompletedJobs()
    {
        var completedJob = CreateJob(stateName: QueueStateTypes.Completed);
        var completedJobId = await _repository.InsertAsync(completedJob);

        var activeJob = CreateJob(stateName: "Enqueued");
        await _repository.InsertAsync(activeJob);

        var prunedCount = await _repository.PruneCompletedJobs();
        Assert.True(prunedCount >= 1);

        var deletedJob = await _repository.GetByIdAsync(completedJobId);
        Assert.Null(deletedJob);
    }

    [Fact]
    public async Task JobRepository_PruneExpiredJobs()
    {
        var expiredJob = CreateJob(stateName: "Enqueued", expiresAt: DateTime.UtcNow.AddMinutes(-5));
        var expiredJobId = await _repository.InsertAsync(expiredJob);

        var activeJob = CreateJob(stateName: "Enqueued", expiresAt: DateTime.UtcNow.AddMinutes(5));
        var activeJobId = await _repository.InsertAsync(activeJob);

        var prunedCount = await _repository.PruneExpiredJobs();
        Assert.True(prunedCount >= 1);

        var deletedJob = await _repository.GetByIdAsync(expiredJobId);
        Assert.Null(deletedJob);

        var active = await _repository.GetByIdAsync(activeJobId);
        Assert.NotNull(active);
    }

    private static Job CreateJob(
        string? stateName = null,
        int retryCount = 0,
        string? jobType = null,
        DateTime? createdAt = null,
        DateTime? scheduledRunAt = null,
        DateTime? expiresAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        var scheduled = scheduledRunAt ?? created.AddMinutes(5);

        return new Job
        {
            AfterActionId = null,
            JobType = jobType ?? "Enqueued",
            TypeName = "LibraryTest.FailTestJob",
            MethodName = "FailTestJob",
            MethodDeclaringTypeName = "LibraryTest.FailTestJob",
            ParameterTypeNamesJson = "[]",
            ArgumentsJson = "[]",
            Queue = "Default",
            stateID = 0,
            StateName = stateName ?? "Enqueued",
            RetryCount = retryCount,
            MaxRetries = 3,
            Priority = 0,
            misfirePolicy = (int)MisfirePolicy.FireOnce,
            CreatedAt = created,
            ScheduledRunAt = scheduled,
            ExpiresAt = expiresAt
        };
    }
}


[Collection("MSSQLHostFixture_Collection")]
[Trait("Provider", "MSSQL")]
public class MsSql_Jobs_repositoryTest : JobRepositoryTest<MsSqlFastJobsHostFixture>
{
   public MsSql_Jobs_repositoryTest(MsSqlFastJobsHostFixture fixture) : base(fixture) { }
}

[Collection("MariaDBHostFixture_Collection")]
[Trait("Provider", "MariaDB")]
public class MariaDB_Jobs_repositoryTest : JobRepositoryTest<MariaDbFastJobsHostFixture>
{
    public MariaDB_Jobs_repositoryTest(MariaDbFastJobsHostFixture fixture) : base(fixture) { }
}