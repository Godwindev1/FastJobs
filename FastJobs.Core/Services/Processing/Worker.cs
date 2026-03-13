using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

//PENDING MODIFICATIONS
public class Worker
{
    private readonly int _workerId;
    
    private readonly CancellationToken _shutdownToken;
    private readonly IServiceScopeFactory serviceScopeFactory; 
    public Worker(int workerId, IServiceScopeFactory serviceScope, CancellationToken shutdownToken)
    {
        _workerId = workerId;
        serviceScopeFactory = serviceScope;
        _shutdownToken = shutdownToken;
    }

    public async Task Run()
    {
        while (!_shutdownToken.IsCancellationRequested)
        {
            //wont always Use Default Queue
            using ( var Scope = new ScopeManager(serviceScopeFactory) )
            {
                var Queue = Scope.Resolve<IQueueRepository>();
                var jobQueue = await Queue.Dequeue(FastJobConstants.DefaultQueue);

                if (jobQueue == null)
                {
                    await Task.Delay(500, _shutdownToken);
                    continue;
                }

                var JobRepo = Scope.Resolve<IJobRepository>();
                var Job = await JobRepo.GetByIdAsync(jobQueue.JobId);

                var ResolvedJob = JobResolver.ResolveFireAndForgetJob(Job);
                if (ResolvedJob == null)
                {
                    await Task.Delay(500, _shutdownToken);
                    continue;
                }

                var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);

                try
                {
                    await ResolvedJob.ExecuteAsync(jobCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // expected during shutdown
                }
                catch (Exception ex)
                {
                    // log + requeue
                }
            } 
        }
    }
}