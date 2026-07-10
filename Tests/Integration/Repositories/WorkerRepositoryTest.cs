using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection("FastjobsCollection")]
public abstract class WorkerRepositoryTest<TFixture>  where TFixture : FastJobsHostFixtureBase 
{
    private readonly TFixture _fixture;
    private readonly IWorkerRepository _repository;

    public WorkerRepositoryTest(TFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IWorkerRepository>();
    }

    [Fact]
    public async Task WorkerRepository_InsertGetUpdateAndDelete()
    {
        var worker = CreateWorker();
        var insertedId = await _repository.InsertAsync(worker);
        Assert.True(insertedId > 0, "Expected a positive auto-generated ID");

        var fetched = await _repository.GetByID(insertedId);
        Assert.NotNull(fetched);
        Assert.Equal(worker.WorkerName, fetched.WorkerName);

        fetched.isSleeping = true;
        fetched.LastHeartbeat = DateTime.UtcNow;
        var updatedRows = await _repository.UpdateAsync(fetched);
        Assert.Equal(1, updatedRows);

        var updated = await _repository.GetByID(insertedId);
        Assert.True(updated.isSleeping);

        var deletedRows = await _repository.DeleteAsync(insertedId);
        Assert.Equal(1, deletedRows);
    }

    [Fact]
    public async Task WorkerRepository_GetAllAndFilterSets()
    {
        await _repository.TruncateAsync();

        var active = CreateWorker("active-worker", isSleeping: false, isCrashed: false);
        var sleeping = CreateWorker("sleeping-worker", isSleeping: true, isCrashed: false);
        var crashed = CreateWorker("crashed-worker", isSleeping: false, isCrashed: true);

        await _repository.InsertAsync(active);
        await _repository.InsertAsync(sleeping);
        await _repository.InsertAsync(crashed);

        var all = await _repository.GetAllAsync();
        var sleepingWorkers = await _repository.GetSleepingAsync();
        var activeWorkers = await _repository.GetActiveAsync();
        var deadWorkers = await _repository.GetDeadWorkersAsync();

        Assert.Contains(all, item => item.WorkerName == active.WorkerName);
        Assert.Contains(sleepingWorkers, item => item.WorkerName == sleeping.WorkerName);
        Assert.Contains(activeWorkers, item => item.WorkerName == active.WorkerName);
        Assert.Contains(deadWorkers, item => item.WorkerName == crashed.WorkerName);
    }

    private static FSTJBS_Worker CreateWorker(string? workerName = null, bool isSleeping = false, bool isCrashed = false)
    {
        return new FSTJBS_Worker
        {
            WorkerName = workerName ?? $"worker-{Guid.NewGuid():N}",
            ThreadName = $"thread-{Guid.NewGuid():N}",
            StartedAt = DateTime.UtcNow,
            isSleeping = isSleeping,
            isCrashed = isCrashed,
            LastHeartbeat = DateTime.UtcNow
        };
    }
}

[Collection("MSSQLHostFixture_Collection")]
[Trait("Provider", "MSSQL")]
public class MsSql_Worker_repositoryTest : WorkerRepositoryTest<MsSqlFastJobsHostFixture>
{
    public MsSql_Worker_repositoryTest(MsSqlFastJobsHostFixture fixture) : base(fixture) { }
}

[Collection("MariaDBHostFixture_Collection")]
[Trait("Provider", "MariaDB")]
public class MariaDB_Worker_repositoryTest : WorkerRepositoryTest<MariaDbFastJobsHostFixture>
{
    public MariaDB_Worker_repositoryTest(MariaDbFastJobsHostFixture fixture) : base(fixture) { }
}