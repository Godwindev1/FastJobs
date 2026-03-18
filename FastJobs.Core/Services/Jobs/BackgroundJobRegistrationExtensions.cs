using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

/// <summary>
/// Extension methods for registering background jobs with the DI container.
/// </summary>
public static class BackgroundJobRegistrationExtensions
{
    private static ServiceProviderJobFactory _jobFactory;

    /// <summary>
    /// Registers background jobs and sets up the job factory.
    /// This should be called during service configuration, before calling UseFastJobs.
    /// </summary>
    /// <example>
    /// services.RegisterBackgroundJobs(register =>
    /// {
    ///     register.AddJob<MyBackgroundJob>("MyCustomJob");
    ///     register.AddJob<AnotherJob>("AnotherKey");
    /// });
    /// </example>
    public static IServiceCollection RegisterBackgroundJobs(
        this IServiceCollection services,
        Action<JobRegistrationBuilder> configureJobs)
    {
        var builder = new JobRegistrationBuilder(services);
        configureJobs(builder);
        
        // Store factory in a static for access during job processing
        var serviceProvider = services.BuildServiceProvider();
        _jobFactory = new ServiceProviderJobFactory(serviceProvider);
        
        services.AddSingleton<IJobFactory>(_jobFactory);
        
        return services;
    }

    /// <summary>
    /// Gets the registered job factory.
    /// Used internally by JobResolver.
    /// </summary>
    internal static ServiceProviderJobFactory GetJobFactory() => _jobFactory 
        ?? throw new InvalidOperationException(
            "Job factory not initialized. Call RegisterBackgroundJobs during service configuration.");
}

/// <summary>
/// Builder for fluently registering background job types.
/// </summary>
public class JobRegistrationBuilder
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, Type> _registrations = new();
    private ServiceProviderJobFactory _factory;

    internal JobRegistrationBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a background job type with the DI container and factory.
    /// </summary>
    /// <typeparam name="TJob">The job implementation type</typeparam>
    /// <param name="jobKey">Unique identifier for this job (stored in Job.TypeName)</param>
    /// <param name="lifetime">Service lifetime (default: Transient)</param>
    public JobRegistrationBuilder AddJob<TJob>(
        string jobKey,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TJob : class, IBackGroundJob
    {
        if (string.IsNullOrWhiteSpace(jobKey))
            throw new ArgumentException("Job key cannot be null or empty", nameof(jobKey));

        _registrations[jobKey] = typeof(TJob);

        // Register in DI container
        var descriptor = new ServiceDescriptor(typeof(TJob), typeof(TJob), lifetime);
        _services.Add(descriptor);

        return this;
    }

    /// <summary>
    /// Registers a background job type with a factory method.
    /// </summary>
    /// <typeparam name="TJob">The job implementation type</typeparam>
    /// <param name="jobKey">Unique identifier for this job</param>
    /// <param name="factory">Factory method to create the job instance</param>
    /// <param name="lifetime">Service lifetime (default: Transient)</param>
    public JobRegistrationBuilder AddJob<TJob>(
        string jobKey,
        Func<IServiceProvider, TJob> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TJob : class, IBackGroundJob
    {
        if (string.IsNullOrWhiteSpace(jobKey))
            throw new ArgumentException("Job key cannot be null or empty", nameof(jobKey));

        _registrations[jobKey] = typeof(TJob);

        // Register in DI container with factory
        var descriptor = new ServiceDescriptor(typeof(TJob), factory, lifetime);
        _services.Add(descriptor);

        return this;
    }

    internal void BuildFactory(ServiceProviderJobFactory factory)
    {
        _factory = factory;
        foreach (var registration in _registrations)
        {
            _factory.RegisterJobType(registration.Key, registration.Value);
        }
    }
}
