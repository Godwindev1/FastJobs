using System.Runtime.CompilerServices;
using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.SqlServer;

namespace Fastjobs.Dashboard;

public class ScheduledJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly IStateHistoryRepository _stateHistoryRepository;

    public ScheduledJobService(IJobRepository jobRepository, IScheduledJobRepository scheduledJobRepository, IStateHistoryRepository stateHistoryRepository)
    {
        _jobRepository = jobRepository;
        _scheduledJobRepository = scheduledJobRepository;
        _stateHistoryRepository = stateHistoryRepository;
    }

    public async Task<IEnumerable<ScheduledJobModel>> GetAllScheduledJobsAsync()
    {
        var scheduledJobs = await _scheduledJobRepository.GetAllAsync();
        
        var result = new List<ScheduledJobModel>();
        foreach(var job in scheduledJobs)
        {
            result.Add(await MapToModel(job));
        }

        return result;
    }


    public async Task<ScheduledJobModel> MapToModel(ScheduledJobInfo scheduledJob)
    {
        var job = await _jobRepository.GetByIdAsync(scheduledJob.JobId);
        var timestamps = await _stateHistoryRepository.GetTimestampsByJobIdAsync(scheduledJob.JobId);

        return new ScheduledJobModel
        {
            Id = scheduledJob.Id,
            JobName = $"{job.MethodDeclaringTypeName}.{job.MethodName}",
            QueueName = job.Queue,
            TypeName = job.TypeName,
            MethodName = job.MethodName,
            EnqueueAt = timestamps.EnqueuedAt ?? DateTime.Now, // Fallback to now if null
            CreatedAt = job.CreatedAt,
            JobType = job.JobType,
            TimeTillScheduledrun = scheduledJob.ScheduledTo - DateTime.UtcNow
        };
    }
}
