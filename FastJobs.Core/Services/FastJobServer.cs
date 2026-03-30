
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



    public static EnqueueOptions<ExpressionFireAndForgetJob> EnqueueJob(Expression<Action> actionExpression)
    {
        var expressionMetadata = ExtractExpressionMetadata(actionExpression);

      
        var job = new Job
        {
            TypeName = typeof(ExpressionFireAndForgetJob).AssemblyQualifiedName,
            MethodName = expressionMetadata.MethodName,
            MethodDeclaringTypeName = expressionMetadata.MethodDeclaringTypeName,
            ParameterTypeNamesJson = expressionMetadata.ParameterTypeNamesJson,
            ArgumentsJson = expressionMetadata.ArgumentsJson,
            Queue = FastJobConstants.DefaultQueue,
            stateID = 0,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = 3,
            Priority = JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow
        };

        return new EnqueueOptions<ExpressionFireAndForgetJob>(job, _serverInstance._ScopeFactory);
    }

    private static ExpressionJobMetadata ExtractExpressionMetadata(Expression<Action> actionExpression)
    {
        if (actionExpression.Body.NodeType != ExpressionType.Call)
        {
            throw new ArgumentException("Lambda expression must contain only a method call.", nameof(actionExpression));
        }

        var methodCall = (MethodCallExpression)actionExpression.Body;

        return new ExpressionJobMetadata
        {
            MethodName = methodCall.Method.Name,
           
            MethodDeclaringTypeName = methodCall.Method.DeclaringType?.AssemblyQualifiedName 
                ?? throw new InvalidOperationException("Method declaring type could not be determined."),
            
            ParameterTypeNamesJson = JsonConvert.SerializeObject(
                methodCall.Arguments.Select(x => x.Type.FullName).ToList()),
            
            ArgumentsJson = JsonConvert.SerializeObject(
            methodCall.Arguments
                .Select(arg =>
                {
                    if (arg is ConstantExpression c)
                        return c.Value;

                    // Compile and invoke anything else to get its runtime value
                    return Expression.Lambda(arg).Compile().DynamicInvoke();
                })
                .ToList())
            };
        
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
            Priority = JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            MethodName = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson = string.Empty,
            ArgumentsJson = string.Empty,
            ExpiresAt = DateTime.UtcNow
        };


        return new EnqueueOptions<TJob>(job, _serverInstance._ScopeFactory);
    }


    //SCHEDULED JOBS 
        public static EnqueueOptions<TJob> ScheduleJob<TJob>() 
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
