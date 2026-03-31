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
            //Race Condition Possible Between HERE
            //wont always Use Default Queue
            if(await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
            {
                await Task.Delay(200, _shutdownToken);
                continue;
            }

            using ( var Scope = new ScopeManager(serviceScopeFactory) )
            {
                
                var JobDetails = await _QueueProcessor.Dequeue(_shutdownToken);
                //HERE

                IJobRepository JobRepo = Scope.Resolve<IJobRepository>();
                Job job = await JobRepo.GetByIdAsync(JobDetails.Item1.JobId);

                var ResolvedJob = JobResolver.ResolveJob(job, Scope);
                
                if (ResolvedJob == null)
                {
                    await Task.Delay(500, _shutdownToken);
                    continue;
                }

                var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
                bool jobSucceeded = false;

                // Set the job context so jobs like ExpressionFireAndForgetJob can access the job metadata
                // Since This is a ScopedService Resolved Within A Scope Inside the Worker Its Thread safe And Does not Interfare Other Workers Setting Contexts for use Asewell
                var jobContext = Scope.Resolve<IJobContext>() as JobContext;
                jobContext.SetJob( job );

                try
                {
                    StateHelpers StateHelper = new StateHelpers(JobRepo, Scope.Resolve<IStateHistoryRepository>());
                    await StateHelper.UpdateJobStateAsync(job.Id, QueueStateTypes.Processing, "Job is being processed", "", jobCts.Token);

                    await ResolvedJob.ExecuteAsync(jobCts.Token);
                    jobSucceeded = true; 
                }
                catch (OperationCanceledException) when (jobCts.IsCancellationRequested)
                {
                    // expected during shutdown
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await _QueueProcessor.RequeueJobAsync(JobDetails.Item1, JobDetails.Item2, ex.Message);
                }

               
                if (jobSucceeded)
                {
                    try
                    {
                        await _QueueProcessor.CompleteJobAsync(JobDetails.Item1, JobDetails.Item2);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't requeue — job work is done, only cleanup failed
                        Console.WriteLine($"CompleteJob failed: {ex.Message}");
                    }

                    jobContext.SetJob( null );
                }

            } 
        }
    }
}