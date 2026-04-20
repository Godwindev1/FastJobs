namespace FastJobs;

public interface IRecurringJobRepository
{
    /// <summary>
    /// Inserts a new recurring job and returns its generated ID.
    /// </summary>
    Task<long> InsertAsync(RecurringJob recurringJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a recurring job by its primary key.
    /// </summary>
    Task<RecurringJob?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all recurring jobs.
    /// </summary>
    Task<IEnumerable<RecurringJob>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all mutable fields of an existing recurring job.
    /// </summary>
    Task<int> UpdateByIdAsync(RecurringJob recurringJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurring job by its primary key.
    /// </summary>
    Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);
}