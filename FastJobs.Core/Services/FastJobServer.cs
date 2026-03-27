
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
    private readonly IServiceScopeFactory _ScopeFactory;
    //Singlton Instance
    static private FastJobServer? _serverInstance = null;

    private FastJobServer(IServiceScopeFactory IserviceScopeFactory)
    {
        _ScopeFactory = IserviceScopeFactory;
    }

    /// <summary>
    /// Create New Instance of FastJobServer 
    /// </summary>
    /// <param name="provider"></param>
    static internal void BuildInstance(IServiceScopeFactory scopeFactory)
    {
        _serverInstance = new FastJobServer(scopeFactory);
    }


    static private FastJobServer GetInstance(IServiceScopeFactory scopeFactory)
    {
        if(_serverInstance != null)
        {
            return _serverInstance;
        }
        else
        {
            BuildInstance(scopeFactory);
            return _serverInstance;   
        }
    }


    public static async Task EnqueueJob(Expression<Action> ActionExpression, CancellationToken cancellationToken = default)
    {
        
        //TODO: To IMPl This FUnction WIll Now Be Responsible For Registering JOBs To DI Container And Enqueuing Jobs To The Database

        using ScopeManager _scopeManager = new ScopeManager(_serverInstance._ScopeFactory);

        var JobRepository = _scopeManager.Resolve<IJobRepository>();
        var stateHistoryRepository = _scopeManager.Resolve<IStateHistoryRepository>();
        var queueRepository = _scopeManager.Resolve<IQueueRepository>();

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


        var JobID = await JobRepository.InsertAsync(job, cancellationToken);
        var State = new State
        {
            JobID = JobID,
            StateName = QueueStateTypes.Enqueued,
            Reason = "Enqueued Job",
            data = "Enqueued Job",
            CreatedAt = DateTime.Now
        };
            
        var StateID = await stateHistoryRepository.InsertAsync(State, cancellationToken);
        
        await JobRepository.UpdateByIdAsync(JobID, "stateID = @stateID, StateName = @StateName", new Job { stateID =  StateID, StateName = QueueStateTypes.Enqueued}, cancellationToken);
        
        //enqueue
        await queueRepository.EnqueueAsync(new Queue { 
            JobId = JobID,
            QueueName = FastJobConstants.DefaultQueue,
            Priority = 2,
            ScheduledAt = DateTime.Now
        }, cancellationToken);
        

        
    }


    public static EnqueueOptions<TJob> EnqueueJob<TJob>() 
        where TJob : class, IBackGroundJob
    {
        var job = new Job
        {
            TypeName = typeof(TJob).AssemblyQualifiedName,
            Queue = FastJobConstants.DefaultQueue,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = 3,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            MethodName = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson = string.Empty,
            ArgumentsJson = string.Empty,
            ExpiresAt = DateTime.UtcNow
        };


        return new EnqueueOptions<TJob>(job, _serverInstance._ScopeFactory);
    }
}
