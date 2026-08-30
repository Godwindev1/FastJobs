

using FastJobs;
using FastJobs.Persistence;
using HostFixtureProviders;
using Microsoft.Extensions.DependencyInjection;

public class RecurringJobSweepJobTestFixture : FastJobsHostFixtureBase
{
    public RecurringJobSweepJobTestFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddJobService<RecurringJobSweepTestJob>();
        services.AddLogging();
        services.AddFastJobs(o => { o.WorkerCount = 1; o.IdleWaitPeriod = TimeSpan.FromSeconds(5);  },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
