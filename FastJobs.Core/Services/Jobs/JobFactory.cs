using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

/// <summary>
/// Factory for resolving background jobs from the DI container.
/// Each job type must be registered in the DI container.
/// </summary>
public interface IJobFactory
{
    /// <summary>
    /// Resolves a job instance by its registered type name.
    /// </summary>
    IBackGroundJob CreateJob(string jobTypeName);
    
    /// <summary>
    /// Registers a job type in the factory for later resolution.
    /// </summary>
    void RegisterJobType(string jobKey, Type jobType);
}

/// <summary>
/// Default implementation using IServiceProvider for dependency resolution.
/// </summary>
internal class ServiceProviderJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _jobTypeRegistry;

    public ServiceProviderJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _jobTypeRegistry = new Dictionary<string, Type>();
    }

    public void RegisterJobType(string jobKey, Type jobType)
    {
        if (!typeof(IBackGroundJob).IsAssignableFrom(jobType))
        {
            throw new ArgumentException(
                $"Job type '{jobType.FullName}' must implement '{nameof(IBackGroundJob)}'",
                nameof(jobType));
        }

        _jobTypeRegistry[jobKey] = jobType;
    }

    public IBackGroundJob CreateJob(string jobTypeName)
    {
        if (!_jobTypeRegistry.TryGetValue(jobTypeName, out var jobType))
        {
            throw new InvalidOperationException(
                $"Job type '{jobTypeName}' is not registered. " +
                $"Ensure the job is registered in the DI container via 'builder.Services.RegisterBackgroundJobs()'");
        }

        var job = _serviceProvider.GetService(jobType);
        if (job == null)
        {
            throw new InvalidOperationException(
                $"Failed to resolve job of type '{jobTypeName}' from the DI container. " +
                $"Ensure it is properly registered.");
        }

        return (IBackGroundJob)job;
    }
}
