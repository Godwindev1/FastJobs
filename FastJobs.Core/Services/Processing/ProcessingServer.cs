using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FastJobs.SqlServer;

namespace FastJobs;

//Processing Server WIll ALso Have to Run on its Own Dedicated Thread So it Does Not Block Client 
internal class ProcessingServer
{
    private ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>> _ScheduledJobs;   
    private JobProcessor _jobProcessor; 
    public ProcessingServer(JobProcessor jobProcessor)
    {
        _jobProcessor = jobProcessor;
        _ScheduledJobs = new ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>>();
    }

    private void ScheduleJob( Tuple<Queue, SessionDatabaseLock> JobDetails)
    {
        //Call JobReflection CLass And Store the BackgroundJob On Concurrent Queue Allong With its DBLock
    }

    public async void ProcessJobs()
    {
        Thread JobProcessingThread = new Thread (
            async () => {
                while(true)
                {
                    //Default Queue is HardCoded Here Until Support For Multi Queue Is Implemented
                    await _jobProcessor.DeQueueItem(FastJobConstants.DefaultQueue);      
                }
            }
        );

        JobProcessingThread.Start();
    }


}