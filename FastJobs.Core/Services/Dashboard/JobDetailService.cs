using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.SqlServer;

namespace FastJobs.Dashboard;

public class JobDetailService
{
      private readonly IJobRepository _jobRepository;
        private readonly IStateHistoryRepository _stateHistoryRepository;
    
        public JobDetailService(IJobRepository jobRepository, IStateHistoryRepository stateHistoryRepository)
        {
            _jobRepository = jobRepository;
            _stateHistoryRepository = stateHistoryRepository;
        }

        public JobState SwitchjobState(string state)
        {
            return state switch
            {
                "Enqueued" => JobState.Enqueued,
                "Processing" => JobState.Processing,
                "Completed" => JobState.Completed,
                "Failed" => JobState.Failed,
                "Scheduled" => JobState.Scheduled,
                "Dequeued" => JobState.Dequeued,
                _ => throw new ArgumentException($"Unknown job state: {state}")
            };
        }
    

        public async Task<JobDetailModel> GetJobDetailsAsync(long jobId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
                throw new KeyNotFoundException($"Job with ID {jobId} not found.");

            return await MapToDetailModelAsync(job);
        }

        public async Task<IEnumerable<JobDetailModel>> GetAllJobDetailsAsync()
        {
            var jobs = await _jobRepository.GetAllAsync();

            var result = new List<JobDetailModel>();
            foreach (var job in jobs)
            {
                result.Add(await MapToDetailModelAsync(job));
            }
            return result;
        }

        // Private conversion abstraction
        private async Task<JobDetailModel> MapToDetailModelAsync(Job job)
        {
            JobTimestamps? timestamps = await _stateHistoryRepository.GetTimestampsByJobIdAsync(job.Id);

            return new JobDetailModel
            {
                Id = job.Id,
                JobName = job.TypeName,
                QueueName = job.Queue,
                State = SwitchjobState(job.StateName),
                CreatedAt = job.CreatedAt,
                EnqueuedAt = timestamps?.EnqueuedAt,
                StartedAt = timestamps?.StartedAt,
                CompletedAt = timestamps?.CompletedAt,
                Duration = timestamps?.StartedAt.HasValue == true && timestamps?.CompletedAt.HasValue == true
                    ? (timestamps.CompletedAt.Value - timestamps.StartedAt.Value)
                    : (TimeSpan?)null,
                AttemptCount = job.RetryCount + 1,
                MethodName = job.MethodName,
                SerializedArguments = job.ArgumentsJson,
            };
        }
}