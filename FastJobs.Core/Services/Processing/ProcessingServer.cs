using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FastJobs.SqlServer;
using Microsoft.EntityFrameworkCore.Internal;

namespace FastJobs;

//Processing Server WIll ALso Have to Run on its Own Dedicated Thread So it Does Not Block Client 
internal class ProcessingServer
{
    private ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>> _ScheduledJobs;   
    private QueueProcessor _jobProcessor; 
    private readonly IJobRepository _JobRepo;
    public ProcessingServer(QueueProcessor jobProcessor, IJobRepository jobRepository)
    {
        _JobRepo = jobRepository;
        _jobProcessor = jobProcessor;
        _ScheduledJobs = new ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>>();
    }

    private async Task ScheduleJob( Tuple<Queue, SessionDatabaseLock> JobDetails)
    {
        //Call JobReflection CLass And Store the BackgroundJob On Concurrent Queue Allong With its DBLock
        var Job =   await _JobRepo.GetByIdAsync(JobDetails.Item1.JobId);
        _ScheduledJobs.Enqueue(
             new Tuple<IBackGroundJob, SessionDatabaseLock>(
                JobResolver.ResolveFireAndForgetJob(Job),
                JobDetails.Item2
            )
        ); 
    }

    public async void StartProcessingJobs()
    {
        Thread JobProcessingThread = new Thread (
            async () => {
                while(true)
                {
                    if(await _jobProcessor.IsQueueEmpty(FastJobConstants.DefaultQueue))
                    {
                        Thread.Sleep(400);
                        continue;
                    }
                    //Default Queue is HardCoded Here Until Support For Multi Queue Is Implemented
                    var result = await _jobProcessor.DeQueueItem(FastJobConstants.DefaultQueue);  
                    
                    if(result != null)
                    await ScheduleJob(result);  
                }
            }
        );

        JobProcessingThread.Name = "FastJobs.ProcessingServer";
        JobProcessingThread.IsBackground = true;
        JobProcessingThread.Start();
    }


}