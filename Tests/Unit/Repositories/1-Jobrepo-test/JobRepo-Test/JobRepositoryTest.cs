
using Microsoft.Extensions.Hosting;
using FastJobs;
using FastJobs.Persistence;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class JobRepositoryTest : IClassFixture<FastJobsHostFixture>
{
    private readonly FastJobsHostFixture _fixture;
    private readonly IJobRepository _repository;
    public JobRepositoryTest(FastJobsHostFixture fixture) 
    {
        _fixture = fixture;
        _repository = fixture.Host.Services.GetRequiredService<IJobRepository>();
    } 

    [Fact]
    public async Task JobRepository_Insert()
    {
        var job = new Job
        {
            AfterActionId = null,                 // Column value: NULL

            JobType = JobTypes.Enqueued,          // Column: Enqueued

            // This whole string looks like the invocation data / type + method.
            // Split into TypeName and MethodName according to your conventions.
            TypeName = "LibraryTest.FailTestJob", // From: FailTestJob, LibraryTest, ...
            MethodName = "FailTestJob",           // Method part

            // Fire-and-forget specific metadata (if applicable).
            MethodDeclaringTypeName = "LibraryTest.FailTestJob",

            // These two are stored as JSON in your design; here we use empty arrays.
            ParameterTypeNamesJson = "[]",
            ArgumentsJson = "[]",

            Queue = "Default",                    // Column: Default

            stateID = 0,                          // Column: 0
            StateName = "Enqueued",              // Column: Enqueued

            RetryCount = 5,                       // Column: 5
            MaxRetries = 3,                       // From default in your class
            Priority = 0,                         // Column: 0

            misfirePolicy = (int)MisfirePolicy.FireOnce,

            CreatedAt = new DateTime(2026, 7, 4, 19, 39, 06, DateTimeKind.Utc),
            ScheduledRunAt = new DateTime(2026, 7, 5, 19, 39, 06, DateTimeKind.Utc),
            ExpiresAt = null                      // Column: 0 → no expiry / map to null
        };

        var ResultID = await _repository.InsertAsync(job);
        Assert.True(ResultID > 0, "Expected a positive auto-generated ID");
    }

    [Fact]
    public async Task JobRepository_GetByID()
    {
                var job = new Job
        {
            AfterActionId = null,                 // Column value: NULL

            JobType = JobTypes.Enqueued,          // Column: Enqueued

            // This whole string looks like the invocation data / type + method.
            // Split into TypeName and MethodName according to your conventions.
            TypeName = "LibraryTest.FailTestJob", // From: FailTestJob, LibraryTest, ...
            MethodName = "FailTestJob",           // Method part

            // Fire-and-forget specific metadata (if applicable).
            MethodDeclaringTypeName = "LibraryTest.FailTestJob",

            // These two are stored as JSON in your design; here we use empty arrays.
            ParameterTypeNamesJson = "[]",
            ArgumentsJson = "[]",

            Queue = "Default",                    // Column: Default

            stateID = 0,                          // Column: 0
            StateName = "Enqueued",              // Column: Enqueued

            RetryCount = 5,                       // Column: 5
            MaxRetries = 3,                       // From default in your class
            Priority = 0,                         // Column: 0

            misfirePolicy = (int)MisfirePolicy.FireOnce,

            CreatedAt = new DateTime(2026, 7, 4, 19, 39, 06, DateTimeKind.Utc),
            ScheduledRunAt = new DateTime(2026, 7, 5, 19, 39, 06, DateTimeKind.Utc),
            ExpiresAt = null                      // Column: 0 → no expiry / map to null
        };

        var ResultID = await _repository.InsertAsync(job);
        Assert.True(ResultID > 0, "Insert Failed Expected a Positive Auto Generate Number ");

        var ResultJob = await _repository.GetByIdAsync(ResultID);
        Assert.NotNull(ResultJob);
    }

    [Fact]
    public void JobRepository_GetAllAsync()
    {
        
    }

    [Fact]
    public void JobRepository_DeleteByIdAsync()
    {

    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JobRepository_UpdateByIdAsync_Two_overloads(bool IsPartialUpdate)
    {
    }

    [Fact]
    public void JobRepository_CountByStateAsync()
    {

    }

    [Fact]
    public void JobRepository_CountRetryingAsync()
    {
        
    }

    [Fact]
    public void JobRepository_CountCompletedSinceAsync()
    {

    }

    [Fact]
    public void JobRepository_CountFailedSinceAsync()
    {

    }

    [Fact]
    public void JobRespository_CountStateBetween()
    {

    }

    [Fact]
    public void JobRepository_GetMisfiredJobsAsync()
    {

    }

    [Fact]
    public void JobRepository_PruneCompletedJobs()
    {

    }

    [Fact]
    public void JobRepository_PruneExpiredJobs()
    {

    }
}