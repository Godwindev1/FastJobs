using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HostFixtureProviders;

public class PostgresFastJobsHostFixture : FastJobsHostFixtureBase
{
    public PostgresFastJobsHostFixture() : base(new PostgresFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddFastJobs(o => o.WorkerCount = 1,
            new FastJobPostgresDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
