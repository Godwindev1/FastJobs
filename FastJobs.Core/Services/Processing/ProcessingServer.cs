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




}