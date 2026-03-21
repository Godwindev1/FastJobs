
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft;
using Newtonsoft.Json;

namespace FastJobs;

public class FastJobServer
{
    //Service Provider For Reolving FastJob Dependencies 
    private readonly IServiceProvider _serviceProvider;
    //Singlton Instance
    static private FastJobServer? _serverInstance = null;

    private FastJobServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Create New Instance of FastJobServer 
    /// </summary>
    /// <param name="provider"></param>
    static internal void BuildInstance(IServiceProvider provider)
    {
        _serverInstance = new FastJobServer(provider);
    }


    static private FastJobServer GetInstance(IServiceProvider provider)
    {
        if(_serverInstance != null)
        {
            return _serverInstance;
        }
        else
        {
            BuildInstance(provider);
            return _serverInstance;   
        }
    }


    public static async Task EnqueueJob(Expression<Action> ActionExpression)
    {
        
        //TODO: To IMPl This FUnction WIll Now Be Responsible For Registering JOBs To DI Container And Enqueuing Jobs To The Database

        var JobRepository = _serverInstance._serviceProvider.GetRequiredService<IJobRepository>();
        var stateHistoryRepository = _serverInstance._serviceProvider.GetRequiredService<IStateHistoryRepository>();
        var queueRepository = _serverInstance._serviceProvider.GetRequiredService<IQueueRepository>();

        //Job Storage
        var Type = typeof(FireAndForgetJobs);
        MethodCallExpression MethodExpression;

        if(ActionExpression.Body.NodeType == ExpressionType.Call )
        {
            MethodExpression   = (MethodCallExpression)ActionExpression.Body;     
        }
        else
        {
            throw new Exception("Lambda Should Contain Only A Method Call ");
        }

        
        Job job = new Job
        {
            TypeName = Type.FullName,
            MethodName = MethodExpression.Method.Name,
            MethodDeclaringTypeName = MethodExpression.Method.DeclaringType.AssemblyQualifiedName,
            ParameterTypeNamesJson = JsonConvert.SerializeObject( MethodExpression.Arguments.Select(x => x.Type.FullName).ToList() ),
            ArgumentsJson = JsonConvert.SerializeObject( MethodExpression.Arguments.Select(x => ((ConstantExpression)x).Value) ),
            Queue = FastJobConstants.DefaultQueue,
            stateID = 0,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = 3,
            Priority = 1,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now
        };


        var JobID = JobRepository.InsertAsync(job).GetAwaiter().GetResult();
        var State = new State
        {
            JobID = JobID,
            StateName = QueueStateTypes.Enqueued,
            Reason = "Enqueued Job",
            data = "Enqueued Job",
            CreatedAt = DateTime.Now
        };
            
        var StateID = await stateHistoryRepository.InsertAsync(State);
        
        await JobRepository.UpdateByIdAsync(JobID, "stateID = @stateID, StateName = @StateName", new Job { stateID =  StateID, StateName = QueueStateTypes.Enqueued});
        
        //enqueue
        await queueRepository.EnqueueAsync(new Queue { 
            JobId = JobID,
            QueueName = FastJobConstants.DefaultQueue,
            Priority = 2,
            ScheduledAt = DateTime.Now
        });
        

        
    }
}