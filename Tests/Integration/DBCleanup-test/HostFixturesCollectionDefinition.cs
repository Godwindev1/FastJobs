
using HostFixtureProviders;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[CollectionDefinition("FastJobServerTests", DisableParallelization = true)]
public class FastJobServerTestsCollection { }



public class ExpiryStrategyMariaDbFastJobsHostFixture : FastJobsHostFixtureBase
{
    public ExpiryStrategyMariaDbFastJobsHostFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        //ADD USED JOBS FOR THIS TEST 
        services.AddJobService<NoOpCompletingJob>();
        //SET CLEANUP STRATEGY
        services.SetCleanupStrategy<ExpiredJobsPruningStrategy>();

        services.AddFastJobs(
            option => {  option.WorkerCount = 2;  option.CleanupInterval = TimeSpan.FromSeconds(5); },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            })
        );
    }
}


public class CompletedStrategyMariaDbFastJobsHostFixture : FastJobsHostFixtureBase
{
    public CompletedStrategyMariaDbFastJobsHostFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        //ADD USED JOBS FOR THIS TEST 
        services.AddJobService<NoOpCompletingJob>();
        //SET CLEANUP STRATEGY
        services.SetCleanupStrategy<CompletedJobsPruningStrategy>();

        services.AddFastJobs(
            option => {  option.WorkerCount = 2;  option.CleanupInterval = TimeSpan.FromSeconds(5); },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            })
        );
    }
}

