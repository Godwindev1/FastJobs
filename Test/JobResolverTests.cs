using System;
using System.Threading;
using System.Threading.Tasks;
using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace FastJobs.Tests;

/// <summary>
/// Unit tests for the DI-based JobResolver implementation.
/// Tests job resolution from the DI container and job execution.
/// </summary>
public class JobResolverTests
{
    /// <summary>
    /// Tests that a job can be instantiated and executed with DI.
    /// </summary>
    [Fact]
    public async Task SendEmailJob_SendsEmail_WhenExecuted()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<TestSendEmailJob>>();

        var job = new TestSendEmailJob(mockEmailService.Object, mockLogger.Object);

        // Act
        await job.ExecuteAsync(CancellationToken.None);

        // Assert - verify the email service was called
        mockEmailService.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// Tests job resolution from the DI container.
    /// </summary>
    [Fact]
    public async Task IntegrationTest_JobResolution_FromDIContainer()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var services = new ServiceCollection();
        
        services.AddSingleton(mockEmailService.Object);
        services.AddSingleton(new Mock<ILogger<TestSendEmailJob>>().Object);
        services.AddScoped<TestSendEmailJob>();
        
        services.RegisterBackgroundJobs(register =>
        {
            register.AddJob<TestSendEmailJob>("EmailJob");
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IJobFactory>();

        // Act
        var job = factory.CreateJob("EmailJob");
        await job.ExecuteAsync(CancellationToken.None);

        // Assert
        mockEmailService.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// Tests that unregistered job types throw appropriate errors.
    /// </summary>
    [Fact]
    public void CreateJob_ThrowsInvalidOperationException_WhenJobNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterBackgroundJobs(register =>
        {
            register.AddJob<TestSendEmailJob>("EmailJob");
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IJobFactory>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateJob("NonExistentJob"));
    }

    /// <summary>
    /// Tests factory method job registration.
    /// </summary>
    [Fact]
    public async Task JobRegistration_WithFactory_InitializesJobCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var testConfig = "test-config-value";

        
        services.RegisterBackgroundJobs(register =>
        {
            register.AddJob<TestConfigurableJob>(
                "ConfigJob",
                factory: sp => new TestConfigurableJob(testConfig),
                lifetime: ServiceLifetime.Transient
            );
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IJobFactory>();

        // Act
        var job = factory.CreateJob("ConfigJob");

        // Assert
        Assert.NotNull(job);
        Assert.IsType<TestConfigurableJob>(job);
    }
}

/// <summary>
/// Test implementations of background jobs for testing.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, CancellationToken token);
}

public interface ILogger<T> { }

public class TestSendEmailJob : IBackGroundJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TestSendEmailJob> _logger;

    public TestSendEmailJob(IEmailService emailService, ILogger<TestSendEmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        await _emailService.SendAsync("test@example.com", "Test Subject", token);
    }
}

public class TestConfigurableJob : IBackGroundJob
{
    private readonly string _config;

    public TestConfigurableJob(string config)
    {
        _config = config;
    }

    public Task ExecuteAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
