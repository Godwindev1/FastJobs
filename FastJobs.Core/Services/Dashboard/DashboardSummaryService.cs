
using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.SqlServer;

namespace Fastjobs.Dashboard;

public class DashboardSummaryService
{
    private readonly IJobRepository _jobRepository;
    private readonly IWorkerRepository _workerRepository;

    FastJobsOptions _options;

    public DashboardSummaryService(IJobRepository jobRepository, IWorkerRepository workerRepository, FastJobsOptions options)
    {
        _jobRepository = jobRepository;
        _workerRepository = workerRepository;
        _options = options;
    }

    public async Task<DashboardSummaryModel> GetDashboardSummaryAsync()
    {
        var summary = new DashboardSummaryModel
        {
            EnqueuedCount = await _jobRepository.CountByStateAsync(QueueStateTypes.Enqueued),
            ScheduledCount = await _jobRepository.CountByStateAsync(QueueStateTypes.Scheduled),
            ProcessingCount = await _jobRepository.CountByStateAsync(QueueStateTypes.Processing),
            SucceededCount = await _jobRepository.CountByStateAsync(QueueStateTypes.Completed),
            FailedCount = await _jobRepository.CountByStateAsync(QueueStateTypes.Failed),
            RetryingCount = await _jobRepository.CountRetryingAsync(),
            TotalJobs = (await _jobRepository.GetAllAsync()).Count,
            ActiveWorkers = (await _workerRepository.GetActiveAsync()).Count,
            SleepingWorkers = (await _workerRepository.GetSleepingAsync()).Count,
            DeadWorkers = (await _workerRepository.GetDeadWorkersAsync()).Count,
            SucceededLastHour = await _jobRepository.CountCompletedSinceAsync(DateTime.UtcNow.AddHours(-1)),
            FailedLastHour = await _jobRepository.CountFailedSinceAsync(DateTime.UtcNow.AddHours(-1)),
            ThroughputPerMinute = await CalculateThroughputPerMinuteAsync(),
            HourlyTrend = await CalculateHourlyTrendAsync(),
            DefaultMaxRetries = _options.DefaultMaxRetries
        };

        return summary;
    }

    private async Task<double> CalculateThroughputPerMinuteAsync()
    {
        int completedLastHour = await _jobRepository.CountCompletedSinceAsync(DateTime.UtcNow.AddHours(-1));
        return completedLastHour / 60.0; // Average per minute
    }

    private async Task<IReadOnlyList<ThroughputBucketModel>> CalculateHourlyTrendAsync()
    {
        var buckets = new List<ThroughputBucketModel>();
        DateTime now = DateTime.UtcNow;

        for (int i = 0; i < 24; i++)
        {
            DateTime hourStart = now.AddHours(-i - 1);
            DateTime hourEnd   = now.AddHours(-i);

            int succeededCount = await _jobRepository.CountStateBetween(QueueStateTypes.Completed, hourStart, hourEnd);
            int failedCount    = await _jobRepository.CountStateBetween(QueueStateTypes.Failed, hourStart, hourEnd);
            int EnqueuedCount    = await _jobRepository.CountStateBetween(QueueStateTypes.Enqueued, hourStart,  hourEnd);

            buckets.Add(new ThroughputBucketModel
            {
                HourStart  = hourStart,
                SucceededCount  = succeededCount,
                FailedCount     = failedCount,
                EnqueuedCount = EnqueuedCount
            });
        }

        // Reverse so index 0 is the oldest hour, 23 is the most recent
        buckets.Reverse();
        return buckets.AsReadOnly();
    }
}