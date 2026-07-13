

using FastJobs;
using FastJobs.Persistence;
using HostFixtureProviders;
using Microsoft.Extensions.DependencyInjection;

public class TerminateJobTestFixture : FastJobsHostFixtureBase
{
    public TerminateJobTestFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddJobService<DeleteAfterActionTestJob>();
        services.AddFastJobs(o => o.WorkerCount = 1,
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
