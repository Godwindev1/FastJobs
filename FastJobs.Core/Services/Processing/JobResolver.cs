namespace FastJobs;

/// <summary>
/// Resolves background jobs using Dependency Injection instead of Reflection.
/// This replaces the previous reflection-based approach with type-safe, DI-friendly job resolution.
/// 
/// Migration Guide:
/// 1. Create job classes implementing IBackGroundJob
/// 2. Call services.RegisterBackgroundJobs(...) during service configuration
/// 3. Store the job key (string) in Job.TypeName instead of the full type name
/// 4. JobResolver will use the DI container to instantiate jobs
/// </summary>
internal static class JobResolver
{
    /// <summary>
    /// Resolves a fire-and-forget job using the DI container.
    /// The job type must have been registered via RegisterBackgroundJobs.
    /// </summary>
    /// <param name="job">The job metadata from the database</param>
    /// <returns>A resolved background job instance</returns>
    /// <exception cref="InvalidOperationException">If the job type is not registered</exception>
    internal static IBackGroundJob ResolveFireAndForgetJob(Job job)
    {
        if (string.IsNullOrEmpty(job.TypeName))
        {
            throw new InvalidOperationException(
                $"Job with ID {job.Id} has no TypeName specified. " +
                "Job.TypeName should contain the registered job key.");
        }

        try
        {
            var factory = BackgroundJobRegistrationExtensions.GetJobFactory();
            return factory.CreateJob(job.TypeName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to resolve job '{job.TypeName}'. " +
                "Ensure it is registered via RegisterBackgroundJobs.",
                ex);
        }
    }
}