using FastJobs;
using FastJobs.Persistence;
using HostFixtureProviders;
using Microsoft.Extensions.DependencyInjection;

public class RecurringAfterActionModeTestFixture : FastJobsHostFixtureBase
{
    public RecurringAfterActionModeTestFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        services.AddJobService<RunPerInstanceModeTestJob>();
        services.AddJobService<RunAfterFinalCompletionModeTestJob>();
        services.AddScoped<PerInstanceTrackingAfterAction>();
        services.AddScoped<FinalCompletionTrackingAfterAction>();

        services.AddFastJobs(o => o.WorkerCount = 1,
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
            }));
    }
}
