using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

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

[CollectionDefinition("MariaDBHostFixture_Collection")]
public class MariaDBCollectionDefinition : ICollectionFixture<MariaDbFastJobsHostFixture>
{
    
}