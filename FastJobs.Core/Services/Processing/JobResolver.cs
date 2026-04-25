namespace FastJobs;
using FastJobs.SqlServer;

/// <summary>
/// Resolves IBackgroundJob implementation types using Dependency Injection.
/// </summary>
internal static class JobResolver
{
    /// <summary>
    /// Resolves a background job instance from the given job metadata.

    /// </summary>
    /// <param name="job">The job metadata from the database</param>
    /// <returns>A resolved background job instance</returns>
    /// <exception cref="InvalidOperationException">If the job type is not registered</exception>
    internal static IBackGroundJob ResolveJob(Job job, ScopeManager scope)
    {
        if (string.IsNullOrEmpty(job.TypeName))
            throw new InvalidOperationException(
                $"Job {job.Id} has no TypeName. Cannot resolve.");

        var jobType = Type.GetType(job.TypeName)
            ?? throw new InvalidOperationException(
                $"Type '{job.TypeName}' could not be found.");

        return scope.Resolve(jobType) as IBackGroundJob
            ?? throw new InvalidOperationException(
                $"'{job.TypeName}' does not implement IBackGroundJob.");
    }
}