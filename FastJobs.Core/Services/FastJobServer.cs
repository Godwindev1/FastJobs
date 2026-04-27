
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using FastJobs.SqlServer;

namespace FastJobs;

public static class FastJobServer
{
    private static IServiceScopeFactory _ScopeFactory;
    private static FastJobsOptions _options;


    static internal void BuildInstance(IServiceScopeFactory scopeFactory)
    {
        _ScopeFactory = scopeFactory;
        _options = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<FastJobsOptions>();
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
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };

        return new EnqueueOptions<ExpressionFireAndForgetJob>(job, _ScopeFactory);
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
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            MethodName = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson = string.Empty,
            ArgumentsJson = string.Empty,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };


        return new EnqueueOptions<TJob>(job, _ScopeFactory);
    }


    //SCHEDULED JOBS 

    public static ScheduledJobOptions<ExpressionFireAndForgetJob> ScheduleJob(Expression<Action> actionExpression)
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
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };

        return new ScheduledJobOptions<ExpressionFireAndForgetJob>(job, _ScopeFactory);
    }


    public static ScheduledJobOptions<TJob> ScheduleJob<TJob>() 
        where TJob : class, IBackGroundJob
    {
        var job = new Job
        {
            TypeName = typeof(TJob).AssemblyQualifiedName,
            Queue = FastJobConstants.DefaultQueue,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.High,
            CreatedAt = DateTime.UtcNow,
            MethodName = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson = string.Empty,
            ArgumentsJson = string.Empty,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };


        return new ScheduledJobOptions<TJob>(job, _ScopeFactory);
    }


    //RECURRING JOBS

    public static RecurringJobOptions<ExpressionFireAndForgetJob> AddRecurringJob(Expression<Action> actionExpression)
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
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };

        return new RecurringJobOptions<ExpressionFireAndForgetJob>(job, _ScopeFactory);
    }



    public static RecurringJobOptions<TJob> AddRecurringJob<TJob>() 
    where TJob : class, IBackGroundJob
    {
        var job = new Job
        {
            TypeName = typeof(TJob).AssemblyQualifiedName,
            Queue = FastJobConstants.DefaultQueue,
            StateName = QueueStateTypes.Enqueued,
            RetryCount = 0,
            MaxRetries = _options.DefaultMaxRetries,
            Priority = (int)JobPriority.High,
            CreatedAt = DateTime.UtcNow,
            MethodName = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson = string.Empty,
            ArgumentsJson = string.Empty,
            ExpiresAt = _options.DefaultJobExpiration == TimeSpan.Zero ? (DateTime?)null : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };


        return new RecurringJobOptions<TJob>(job, _ScopeFactory);
    }

    

}
