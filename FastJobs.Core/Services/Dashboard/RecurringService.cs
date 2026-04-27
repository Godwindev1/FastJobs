
using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.SqlServer;
using CronExpressionDescriptor;
namespace Fastjobs.Dashboard;

public class RecurringJobService
{
    private readonly IRecurringJobRepository _recurringJobRepository;
    private readonly IStateHistoryRepository _stateHistoryRepository;
    private readonly IJobRepository _jobRepository;

    public RecurringJobService(IRecurringJobRepository recurringJobRepository, IStateHistoryRepository stateHistoryRepository, IJobRepository jobRepository)
    {
        _recurringJobRepository = recurringJobRepository;
        _stateHistoryRepository = stateHistoryRepository;
        _jobRepository = jobRepository;
    }

    public async Task<IEnumerable<RecurringJobModel>> GetAllRecurringJobsAsync()
    {
        var recurringJobs = await _recurringJobRepository.GetAllAsync();

        var result = new List<RecurringJobModel>();
        foreach (var job in recurringJobs)
        {
            result.Add(await MapToModelAsync(job));
        }
        return result;
    }

    // Private conversion abstraction
    private async Task<RecurringJobModel> MapToModelAsync(RecurringJob job)
    {
        JobTimestamps? timestamps = await _stateHistoryRepository.GetTimestampsByJobIdAsync(job.JobId);
        var JobStore = await _jobRepository.GetByIdAsync(job.JobId);

        var options = new CronExpressionDescriptor.Options
        {
            ThrowExceptionOnParseError = false,
            Verbose = false
        };

        return new RecurringJobModel
        {
            Id = job.id,
            DisplayName = $"{JobStore.MethodDeclaringTypeName}.{JobStore.MethodName}",
            TypeName = JobStore.TypeName,
            MethodName = JobStore.MethodName,
            QueueName = JobStore.Queue,
            ScheduleType = job.IsCron ? ScheduleType.Cron : ScheduleType.Interval,
            CronExpression = job.CronExpression,
            CronDescription = ExpressionDescriptor.GetDescription(job.CronExpression, options), // 👈 pass options
            Interval = job.IntervalTicks.HasValue ? TimeSpan.FromTicks(job.IntervalTicks.Value) : null,
            TimeZoneId = "UTC",
            Status = JobStore.ExpiresAt > DateTime.UtcNow ? RecurringJobStatus.Active : RecurringJobStatus.Disabled,
            NextRunAt = job.NextScheduledTime,
            LastRunAt = timestamps?.StartedAt,
            RegisteredAt = job.StartTime
        };
    }
}
