using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HostFixtureProviders;

public class MariaDbFastJobsHostFixture : FastJobsHostFixtureBase
{
    public MariaDbFastJobsHostFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddFastJobs(o => o.WorkerCount = 1,
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}

