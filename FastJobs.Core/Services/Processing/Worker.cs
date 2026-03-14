using System.Reflection.Metadata;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

//PENDING MODIFICATIONS
public class Worker
{
    private readonly int _workerId;
    
    private readonly CancellationToken _shutdownToken;
    private readonly IServiceScopeFactory serviceScopeFactory; 

    private readonly QueueProcessor _QueueProcessor;
    public Worker(int workerId, IServiceScopeFactory serviceScope, CancellationToken shutdownToken)
    {
        using var Scopemanager = new ScopeManager(serviceScope);
        
        _QueueProcessor = new QueueProcessor(
            Scopemanager.Resolve<IQueueRepository>(),
            Scopemanager.Resolve<IJobRepository>(),
            Scopemanager.Resolve<IStateHistoryRepository>(),
            Scopemanager.Resolve<LockProvider>()
        );

        _workerId = workerId;
        serviceScopeFactory = serviceScope;
        _shutdownToken = shutdownToken;
    }

    public async Task Run()
    {
        while (!_shutdownToken.IsCancellationRequested)
        {
            //wont always Use Default Queue
            if(await _QueueProcessor.IsQueueEmpty(FastJobConstants.DefaultQueue))
            {
                await Task.Delay(500, _shutdownToken);
                continue;
            }

            using ( var Scope = new ScopeManager(serviceScopeFactory) )
            {
                var JobDetails = await _QueueProcessor.DequeueAsync(FastJobConstants.DefaultQueue);

                IJobRepository JobRepo = Scope.Resolve<IJobRepository>();
                Job job = await JobRepo.GetByIdAsync(JobDetails.Item1.JobId);

                var ResolvedJob = JobResolver.ResolveFireAndForgetJob(job);
                
                if (ResolvedJob == null)
                {
                    await Task.Delay(500, _shutdownToken);
                    continue;
                }

                var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);

                try
                {
                    await ResolvedJob.ExecuteAsync(jobCts.Token);
                    //When Job Completes release Lock And Update Job State
                    await _QueueProcessor.CompleteJobAsync(JobDetails.Item1, JobDetails.Item2); 
                }
                catch (OperationCanceledException)
                {
                    // expected during shutdown
                    // Possible implementation of JobStates system Store Persist Progress of Processing Jobs 
                }
                catch (Exception ex)
                {
                    // log + requeue
                    Console.WriteLine(ex.Message);
                    await _QueueProcessor.RequeueJobAsync(JobDetails.Item1, JobDetails.Item2, ex.Message);
                }
            } 
        }
    }
}