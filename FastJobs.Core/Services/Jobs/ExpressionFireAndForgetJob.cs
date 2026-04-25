using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using FastJobs.SqlServer;

namespace FastJobs;


public interface IJobContext
{
    Job? CurrentJob { get; }
}

public class JobContext : IJobContext
{
    public Job? CurrentJob { get; private set; }

    public void SetJob(Job job) => CurrentJob = job;
}
/// <summary>
/// A fire-and-forget job that executes expression-based method calls.
/// Follows Single Responsibility by deferring expression execution to IExpressionResolver.
/// Retrieves job metadata from the scoped IJobContext set by the Worker.
/// </summary>
public class ExpressionFireAndForgetJob : IBackGroundJob
{
    private readonly IExpressionResolver _expressionResolver;
    private readonly IJobContext _jobContext;

    public ExpressionFireAndForgetJob(IExpressionResolver expressionResolver, IJobContext jobContext)
    {
        _expressionResolver = expressionResolver;
        _jobContext = jobContext;
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        if (_jobContext.CurrentJob == null)
        {
            throw new InvalidOperationException(
                "JobContext.CurrentJob was not set. Worker must set the job context before executing.");
        }

        await _expressionResolver.ExecuteAsync(token);
    }
}

/// <summary>
/// Responsible for resolving and executing expressions with DI support.
/// This service handles expression parsing and execution concerns.
/// </summary>
public interface IExpressionResolver
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation of expression resolution.
/// Reads expression metadata from the job context and executes via reflection and DI.
/// </summary>
public class DefaultExpressionResolver : IExpressionResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobContext _jobContext;

    public DefaultExpressionResolver(IServiceScopeFactory scopeFactory, IJobContext jobContext)
    {
        _scopeFactory = scopeFactory;
        _jobContext = jobContext;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var job = _jobContext.CurrentJob
            ?? throw new InvalidOperationException("JobContext.CurrentJob is not available.");

        try
        {
            var metadata = ExpressionJobMetadata.FromJob(job);
            var declaringType = Type.GetType(metadata.MethodDeclaringTypeName)
                ?? throw new InvalidOperationException(
                    $"Type '{metadata.MethodDeclaringTypeName}' could not be found.");

            var parameterTypes = metadata.GetParameterTypes();
            var method = declaringType.GetMethod(metadata.MethodName, parameterTypes)
                ?? throw new InvalidOperationException(
                    $"Method '{metadata.MethodName}' with matching signature not found on type '{declaringType.Name}'.");

            object? instance = null;

            // Attempt to resolve from DI container first
            if (!method.IsStatic)
            {
                using var scope = _scopeFactory.CreateScope();
                instance = scope.ServiceProvider.GetService(declaringType);

                // Fallback to Activator if not in DI
                instance ??= Activator.CreateInstance(declaringType);
            }

            var arguments = metadata.GetArguments();

            if (method.ReturnType == typeof(Task) || 
                (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                var task = (Task?)method.Invoke(instance, arguments)
                    ?? throw new InvalidOperationException($"Method '{metadata.MethodName}' returned null.");
                
                await task;
            }
            else
            {
                method.Invoke(instance, arguments);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to execute expression: {job.MethodName} on {job.MethodDeclaringTypeName}", ex);
        }
    }
}


public class ExpressionJobMetadata
{
    public string MethodName { get; set; } = string.Empty;

    public string MethodDeclaringTypeName { get; set; } = string.Empty;

    public string ParameterTypeNamesJson { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = string.Empty;


    public static ExpressionJobMetadata FromJob(Job job)
    {
        return new ExpressionJobMetadata
        {
            MethodName = job.MethodName,
            MethodDeclaringTypeName = job.MethodDeclaringTypeName,
            ParameterTypeNamesJson = job.ParameterTypeNamesJson,
            ArgumentsJson = job.ArgumentsJson
        };
    }

    /// <summary>
    /// Extract parameter types from serialized metadata
    /// </summary>
    public Type[] GetParameterTypes()
    {
        if (string.IsNullOrEmpty(ParameterTypeNamesJson))
            return Type.EmptyTypes;

        var typeNames = JsonConvert.DeserializeObject<List<string>>(ParameterTypeNamesJson)
            ?? new List<string>();

        return typeNames
            .Select(tn => Type.GetType(tn) ?? throw new InvalidOperationException($"Type '{tn}' not found."))
            .ToArray();
    }

    /// <summary>
    /// Extract arguments from serialized metadata
    /// </summary>
    public object?[] GetArguments()
    {
        if (string.IsNullOrEmpty(ArgumentsJson))
            return Array.Empty<object>();

        var args = JsonConvert.DeserializeObject<List<object?>>(ArgumentsJson)
            ?? new List<object?>();

        return args.ToArray();
    }
}
