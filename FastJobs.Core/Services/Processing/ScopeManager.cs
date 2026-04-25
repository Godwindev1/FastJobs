using Microsoft.Extensions.DependencyInjection;


namespace FastJobs;

/// <summary>
/// A Helper Class used To Enforce DI Scopes Within FastJobs 
/// </summary>
internal sealed class ScopeManager : IDisposable
{
    private readonly IServiceScope _scope;

    private IServiceProvider ServiceProvider => _scope.ServiceProvider;

    internal ScopeManager(IServiceScopeFactory scopeFactory)
    {
        _scope = scopeFactory.CreateScope();
    }

    internal object Resolve(Type type)
    {
        return ServiceProvider.GetRequiredService(type);
    }

    public T Resolve<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}