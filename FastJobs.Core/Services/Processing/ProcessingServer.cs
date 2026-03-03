using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FastJobs;

public class ProcessingServer
{
    private ConcurrentQueue<IBackGroundJob> _ScheduledJobs;    
    public ProcessingServer()
    {
    }

    private Queue FetchJpb()
    {
        return new Queue {  };
    }

    private void ScheduleJobInstance()
    {
        //Add Job instance To _scheduledJobs
    }

    public void ProcessLoop ()
    {
        
    }


}