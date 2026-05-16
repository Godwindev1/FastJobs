namespace FastJobs;
using FastJobs.SqlServer;

/// <summary>
/// Resolves IAfterAction implementation types using Dependency Injection.
/// </summary>
internal static class AfterActionsResolver
{
    /// <summary>
    /// Resolves an AfterAction instance from the given job metadata.

    /// </summary>
    /// <param name="ActionModel">The job metadata from the database</param>
    /// <returns>A resolved AfterAction  instance</returns>
    /// <exception cref="InvalidOperationException">If the Actions type is not registered</exception>
    internal static IAfterAction ResolveAction(AfterActionModel model, ScopeManager scope)
    {
        if (string.IsNullOrEmpty(model.TypeName))
            throw new InvalidOperationException(
                $"After Action {model.Id} has no TypeName. Cannot resolve.");

        var jobType = Type.GetType(model.TypeName)
            ?? throw new InvalidOperationException(
                $"Type For After Action '{model.TypeName}' could not be found.");

        return scope.Resolve(jobType) as IAfterAction
            ?? throw new InvalidOperationException(
                $"'{model.TypeName}' does not implement IAfterAction.");
    }
}