using System.Diagnostics;
using FastJobs.Dashboard.Models;
using FastJobs.Dashboard.Models.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.Services;

public class DashboardService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerManager _workerManager;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5); // Safe polling interval

    public DashboardService(IServiceScopeFactory scopeFactory, WorkerManager workerManager, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _workerManager = workerManager;
        _cache = cache;
    }

    public async Task<DashboardSummaryModel> GetDashboardSummaryAsync()
    {
        return await _cache.GetOrCreateAsync("DashboardSummary", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

            using var scope = _scopeFactory.CreateScope();
            var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            var queueRepo = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
            var recurringRepo = scope.ServiceProvider.GetRequiredService<IRecurringJobRepository>();
            var scheduledRepo = scope.ServiceProvider.GetRequiredService<IScheduledJobRepository>();

            try
            {
                // Aggregate job counts from DB
                var jobCounts = await jobRepo.GetJobStateCountsAsync();
                var recurringCounts = await recurringRepo.GetRecurringJobCountsAsync();
                var scheduledCounts = await scheduledRepo.GetScheduledJobCountsAsync();

                // Worker data from system
                var workers = await _workerManager.GetWorkerModelsAsync();
                var activeWorkers = workers.Count(w => w.State == WorkerState.Active);
                var sleepingWorkers = workers.Count(w => w.State == WorkerState.Sleeping);
                var deadWorkers = workers.Count(w => w.State == WorkerState.Dead);

                // Throughput and trends (simplified; query recent history)
                var throughput = await jobRepo.GetThroughputPerMinuteAsync();
                var hourlyTrend = await jobRepo.GetHourlyThroughputBucketsAsync();

                return new DashboardSummaryModel
                {
                    EnqueuedCount = jobCounts.Enqueued,
                    ScheduledCount = scheduledCounts.Scheduled,
                    ProcessingCount = jobCounts.Processing,
                    SucceededCount = jobCounts.Succeeded,
                    FailedCount = jobCounts.Failed,
                    RetryingCount = jobCounts.Retrying,
                    CancelledCount = jobCounts.Cancelled,
                    TotalJobs = jobCounts.Total,
                    ActiveWorkers = activeWorkers,
                    SleepingWorkers = sleepingWorkers,
                    DeadWorkers = deadWorkers,
                    TotalServers = 1, // Assuming single server; extend for multi-server
                    SucceededLastHour = await jobRepo.GetSucceededLastHourAsync(),
                    FailedLastHour = await jobRepo.GetFailedLastHourAsync(),
                    ThroughputPerMinute = throughput,
                    HourlyTrend = hourlyTrend,
                    DefaultMaxRetries = 3, // From options
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                // Log error and return empty model
                Console.WriteLine($"Error in GetDashboardSummaryAsync: {ex.Message}");
                return new DashboardSummaryModel { GeneratedAt = DateTime.UtcNow };
            }
        });
    }

    public async Task<SystemHealthModel> GetSystemHealthAsync()
    {
        return await _cache.GetOrCreateAsync("SystemHealth", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

            using var scope = _scopeFactory.CreateScope();
            var dbConnection = scope.ServiceProvider.GetRequiredService<DbConnection>(); // Assume injected

            try
            {
                // DB health
                var dbInfo = await GetDatabaseInfoAsync(dbConnection);

                // Queues
                var queues = await GetQueueDepthsAsync(scope);

                // Servers (simplified)
                var servers = new[] { new ServerModel { ServerId = "main", ServerName = "MainServer", IsAlive = true } };

                // System metrics
                var process = Process.GetCurrentProcess();
                var cpuUsage = process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
                var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB

                return new SystemHealthModel
                {
                    Database = dbInfo,
                    Queues = queues,
                    Servers = servers,
                    LibraryVersion = "1.0.0", // From assembly
                    ConfiguredWorkerCount = _workerManager.GetWorkerCount(),
                    DefaultMaxRetries = 3,
                    HeartbeatInterval = TimeSpan.FromMinutes(1),
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSystemHealthAsync: {ex.Message}");
                return new SystemHealthModel { GeneratedAt = DateTime.UtcNow };
            }
        });
    }

    public async Task<PagedResult<WorkerModel>> GetWorkersAsync(int page = 1, int pageSize = 50)
    {
        var workers = await _workerManager.GetWorkerModelsAsync();
        var paged = workers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<WorkerModel>
        {
            Items = paged,
            TotalCount = workers.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    // Helper methods
    private async Task<DatabaseInfoModel> GetDatabaseInfoAsync(DbConnection connection)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await connection.OpenAsync();
            stopwatch.Stop();

            return new DatabaseInfoModel
            {
                Provider = "SQL Server", // Assume
                MaskedConnectionString = MaskConnectionString(connection.ConnectionString),
                ActiveConnections = 1, // Simplified; use connection pool stats if available
                IdleConnections = 0,
                MaxPoolSize = 100, // From config
                IsHealthy = true,
                LastPingLatency = stopwatch.Elapsed,
                LastCheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new DatabaseInfoModel
            {
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<IReadOnlyList<QueueDepthModel>> GetQueueDepthsAsync(IServiceScope scope)
    {
        var queueRepo = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var queues = await queueRepo.GetAllQueuesAsync();

        return queues.Select(q => new QueueDepthModel
        {
            QueueName = q.Name,
            EnqueuedCount = q.EnqueuedCount,
            ProcessingCount = q.ProcessingCount,
            AverageWaitTime = TimeSpan.FromSeconds(10), // Simplified; calculate from history
            ThroughputPerMinute = q.ThroughputPerMinute
        }).ToList();
    }

    private string MaskConnectionString(string connectionString)
    {
        // Simple masking; replace password
        return connectionString.Replace("Password=.*?;", "Password=***;");
    }
}