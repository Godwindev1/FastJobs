using FastJobs;
using FastJobs.Persistence;
using HostFixtureProviders;
using Microsoft.Extensions.DependencyInjection;

public class JobChainTestFixture : FastJobsHostFixtureBase
{
    public JobChainTestFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IChainExecutionRecorder, ChainExecutionRecorder>();
        services.AddJobService<ChainStepAJob>();
        services.AddJobService<ChainStepBJob>();
        services.AddJobService<ChainStepCJob>();
        services.AddJobService<ChainStepDJob>();

        services.AddFastJobs(
            option => { option.WorkerCount = 2; },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            })
        );
    }
}