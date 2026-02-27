using System.Data;
using System.Threading.Tasks;

namespace FastJobs;

public class QueueRepositoryTest
{
    private  IQueueRepository Testobject;
    internal List<Tuple<bool, string>> TestResults  = new List<Tuple<bool, string>>();

    public QueueRepositoryTest( IQueueRepository repo )
    {
        Testobject = repo;
    }


    internal async Task<Queue?> Enqueue(Queue queue)
    {
        var QueueID = await Testobject.EnqueueAsync(queue);
        var QueueEntry = await Testobject.GetQueueEntry(QueueID);
        return QueueEntry;
    }

    internal  async Task<bool> GetQueueEntry(long id)
    {
        
        if(await Testobject.GetQueueEntry(id) == null)
        {
            return false;
        }

        return true;
    }


    internal async Task<bool> RemoveAsync(long id)
    {
        var Result = await Testobject.RemoveAsync(id);
        return Result;
    }


}