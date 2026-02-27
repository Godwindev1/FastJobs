
using System.Diagnostics;
using System.Linq.Expressions;
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


    public static void EnqueueJob(Expression<Action> ActionExpression)
    {
        FireAndForgetJobs fireAndForget = new FireAndForgetJobs(ActionExpression);
        var JobRepository = ActivatorUtilities.CreateInstance<JobRepository>(_serverInstance._serviceProvider);

        //Job Storage
        var Type = fireAndForget.GetType();
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
            MethodDeclaringTypeName = MethodExpression.Method.DeclaringType.FullName,
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


        JobRepository.InsertAsync(job).GetAwaiter().GetResult();

        //TODO: have Dedicated Background Workers For Procession Later On 
        fireAndForget.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}