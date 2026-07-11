
using HostFixtureProviders;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

[CollectionDefinition("FastJobServerTests", DisableParallelization = true)]
public class FastJobServerTestsCollection { }



public class MisfireTestFixture : FastJobsHostFixtureBase
{
    public MisfireTestFixture() : base(new MariaDBFixture()) { }

    protected override void ConfigureFastJobs(IServiceCollection services, string connectionString)
    {
        //ADD USED JOBS FOR THIS TEST 
        services.AddJobService<MisfireTestJob>();

        services.AddFastJobs(
            option => {  option.WorkerCount = 1; option.MisfireDetectorInterval = TimeSpan.FromSeconds(4); option.MisfireDetectorStartupDelay = TimeSpan.FromSeconds(2); option.MisfireThreshold =  TimeSpan.FromSeconds(1); },
            new FastJobMysqlDependencies(x =>
            {
                x.ConnectionString = connectionString;
                x.SchemaName = "FastjobsDB";
                
            })
        );
    }
}

