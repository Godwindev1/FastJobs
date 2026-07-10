namespace FastJobs;

public interface ICleanupStrategy
{
    public Task Clean(CancellationToken cancellationToken);
}

