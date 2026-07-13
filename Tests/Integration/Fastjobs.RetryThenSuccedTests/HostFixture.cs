

using FastJobs;
using FastJobs.Persistence;
using HostFixtureProviders;
using Microsoft.Extensions.DependencyInjection;

public class RetryThenSuccedFixture : FastJobsHostFixtureBase
{
    public RetryThenSuccedFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddJobService<RetryThenSucceedJob>();
        services.AddFastJobs(o => { o.WorkerCount = 1; o.Jitter = TimeSpan.FromSeconds(2); o.JobRetryDelayBase = TimeSpan.FromSeconds(5); },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
