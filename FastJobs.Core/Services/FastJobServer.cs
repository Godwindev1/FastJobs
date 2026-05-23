
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

    // ── Job Template Factory (DRY) ────────────────────────────────────────────────

    internal static Job CreateJobTemplate<TJob>() where TJob : class, IBackGroundJob =>
        new Job
        {
            TypeName                = typeof(TJob).AssemblyQualifiedName,
            Queue                   = FastJobConstants.DefaultQueue,
            StateName               = QueueStateTypes.Enqueued,
            RetryCount              = 0,
            MaxRetries              = _options.DefaultMaxRetries,
            Priority                = (int)JobPriority.Normal,
            CreatedAt               = DateTime.UtcNow,
            MethodName              = string.Empty,
            MethodDeclaringTypeName = string.Empty,
            ParameterTypeNamesJson  = string.Empty,
            ArgumentsJson           = string.Empty,
            ExpiresAt               = _options.DefaultJobExpiration == TimeSpan.Zero
                                        ? (DateTime?)null
                                        : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };

    internal static Job CreateJobTemplate(Expression<Action> actionExpression)
    {
        var metadata = ExtractExpressionMetadata(actionExpression);

        return new Job
        {
            TypeName                = typeof(ExpressionFireAndForgetJob).AssemblyQualifiedName,
            Queue                   = FastJobConstants.DefaultQueue,
            StateName               = QueueStateTypes.Enqueued,
            RetryCount              = 0,
            MaxRetries              = _options.DefaultMaxRetries,
            Priority                = (int)JobPriority.Normal,
            CreatedAt               = DateTime.UtcNow,
            stateID                 = 0,
            MethodName              = metadata.MethodName,
            MethodDeclaringTypeName = metadata.MethodDeclaringTypeName,
            ParameterTypeNamesJson  = metadata.ParameterTypeNamesJson,
            ArgumentsJson           = metadata.ArgumentsJson,
            ExpiresAt               = _options.DefaultJobExpiration == TimeSpan.Zero
                                        ? (DateTime?)null
                                        : DateTime.UtcNow.Add(_options.DefaultJobExpiration)
        };
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


 
    // ── Fire and Forget ───────────────────────────────────────────────────────────
    public static EnqueueOptions<ExpressionFireAndForgetJob> EnqueueJob(Expression<Action> actionExpression) =>
        new EnqueueOptions<ExpressionFireAndForgetJob>(CreateJobTemplate(actionExpression), _ScopeFactory);

    public static EnqueueOptions<TJob> EnqueueJob<TJob>() where TJob : class, IBackGroundJob =>
        new EnqueueOptions<TJob>(CreateJobTemplate<TJob>(), _ScopeFactory);

    // ── Scheduled ─────────────────────────────────────────────────────────────────
    public static ScheduledJobOptions<ExpressionFireAndForgetJob> ScheduleJob(Expression<Action> actionExpression) =>
        new ScheduledJobOptions<ExpressionFireAndForgetJob>(CreateJobTemplate(actionExpression), _ScopeFactory);

    public static ScheduledJobOptions<TJob> ScheduleJob<TJob>() where TJob : class, IBackGroundJob =>
        new ScheduledJobOptions<TJob>(CreateJobTemplate<TJob>(), _ScopeFactory);

    // ── Recurring ─────────────────────────────────────────────────────────────────
    public static RecurringJobOptions<ExpressionFireAndForgetJob> AddRecurringJob(Expression<Action> actionExpression) =>
        new RecurringJobOptions<ExpressionFireAndForgetJob>(CreateJobTemplate(actionExpression), _ScopeFactory);

    public static RecurringJobOptions<TJob> AddRecurringJob<TJob>() where TJob : class, IBackGroundJob =>
        new RecurringJobOptions<TJob>(CreateJobTemplate<TJob>(), _ScopeFactory);


    

}
