
namespace FastJobs;

/// <summary>
/// Provides decoupled helper functions for managing job state transitions and state history.
/// Handles atomicity of state changes by ensuring StateHistory entries and Job updates are coordinated.
/// </summary>
public class StateHelpers
{
    private readonly IJobRepository _jobRepository;
    private readonly IStateHistoryRepository _stateHistoryRepository;

    public StateHelpers(IJobRepository jobRepository, IStateHistoryRepository stateHistoryRepository)
    {
        _jobRepository = jobRepository;
        _stateHistoryRepository = stateHistoryRepository;
    }

    /// <summary>
    /// Updates a job's state and creates a corresponding state history entry atomically.
    /// 
    /// This method performs the following operations in order:
    /// 1. Retrieves the current job by ID
    /// 2. Creates a StateHistory entry with the new state information
    /// 3. Updates the Job with the new state and the new StateHistory ID
    /// 
    /// If any operation fails, the method attempts to rollback previous changes before rethrowing the exception.
    /// In case of rollback failure, a composite exception is thrown containing both the original error and rollback error.
    /// </summary>
    /// <param name="jobId">The ID of the job to update</param>
    /// <param name="newStateName">The new state name (e.g., QueueStateTypes.Completed, QueueStateTypes.Failed)</param>
    /// <param name="reason">The reason for the state change (e.g., "Job Has Been Completed", "Max Retries Exceeded")</param>
    /// <param name="data">Additional context data associated with the state change (e.g., exception message)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The ID of the created StateHistory entry</returns>
    /// <exception cref="InvalidOperationException">Thrown if the job is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if the Job update fails</exception>
    /// <exception cref="InvalidOperationException">Thrown if state creation fails or rollback fails</exception>
    public async Task<long> UpdateJobStateAsync(
        long jobId,
        string newStateName,
        string reason,
        string data = "",
        CancellationToken cancellationToken = default)
    {
        long createdStateHistoryId = -1;
        Job? originalJob = null;

        try
        {
            originalJob = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
            
            if (originalJob == null)
            {
                throw new InvalidOperationException($"Job with ID {jobId} not found. Cannot update state of non-existent job.");
            }

            var stateEntry = new State
            {
                JobID = jobId,
                StateName = newStateName,
                Reason = reason,
                data = data,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                createdStateHistoryId = await _stateHistoryRepository.InsertAsync(stateEntry, cancellationToken);
            }
            catch (Exception stateEx)
            {
                throw new InvalidOperationException(
                    $"Failed to create StateHistory entry for job {jobId}. State: {newStateName}, Reason: {reason}",
                    stateEx);
            }

            // Step 3: Update Job with new state and state ID
            var jobToUpdate = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
            
            if (jobToUpdate == null)
            {
                throw new InvalidOperationException($"Job with ID {jobId} was deleted between read and write operations.");
            }

            jobToUpdate.StateName = newStateName;
            jobToUpdate.stateID = createdStateHistoryId;

            try
            {
                var updateResult = await _jobRepository.UpdateByIdAsync(jobToUpdate, cancellationToken);

                if (updateResult == 0)
                {
                    throw new InvalidOperationException($"Job update returned 0 rows affected. Job ID: {jobId}");
                }
            }
            catch (Exception jobUpdateEx)
            {
                throw new InvalidOperationException(
                    $"Failed to update job {jobId} with new state. Created StateHistory ID: {createdStateHistoryId}. Manual cleanup may be required.",
                    jobUpdateEx);
            }

            return createdStateHistoryId;
        }
        catch (Exception ex)
        {
            // Attempt rollback if StateHistory was successfully created but Job update failed
            if (createdStateHistoryId > 0)
            {
                try
                {
                    await AttemptStateHistoryRollbackAsync(createdStateHistoryId, cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    // Rollback failed aswell 
                    throw new InvalidOperationException(
                        $"Critical Error: Failed to update job state and rollback also failed. " +
                        $"Job ID: {jobId}. Created StateHistory ID: {createdStateHistoryId} (remains in database and needs manual cleanup). " +
                        $"Original Error: {ex.Message}",
                        new AggregateException("Rollback failed", ex, rollbackEx));
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Attempts to rollback a StateHistory entry that was created but whose operation failed.
    /// 
    /// Uses soft delete: marks the StateHistory entry with a DeletedAt timestamp rather than removing it.
    /// This preserves the entry for audit purposes while removing it from active state transition queries.
    /// Maintains a complete audit trail including failed and rolled-back state transitions.
    /// </summary>
    private async Task AttemptStateHistoryRollbackAsync(long stateHistoryId, CancellationToken cancellationToken)
    {
        var rowsAffected = await _stateHistoryRepository.SoftDeleteByIdAsync(stateHistoryId, cancellationToken);
        
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Rollback failed: StateHistory entry {stateHistoryId} was not found or was already soft-deleted.");
        }
    }

    /// <summary>
    /// Validates that the provided state name is a known state type.
    /// </summary>
    /// <param name="stateName">The state name to validate</param>
    /// <returns>true if valid, false otherwise</returns>
    public bool IsValidStateName(string stateName)
    {
        return stateName == QueueStateTypes.Enqueued ||
               stateName == QueueStateTypes.Scheduled ||
               stateName == QueueStateTypes.Processing ||
               stateName == QueueStateTypes.Completed ||
               stateName == QueueStateTypes.Failed;
    }
}
