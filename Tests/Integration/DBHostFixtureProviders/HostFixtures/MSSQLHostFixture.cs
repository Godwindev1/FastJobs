using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HostFixtureProviders;

public class MsSqlFastJobsHostFixture : FastJobsHostFixtureBase
{
    public MsSqlFastJobsHostFixture() : base(new MsSqlFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddFastJobs(o => o.WorkerCount = 1,
            new FastJobMSSQLDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
