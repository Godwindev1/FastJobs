namespace FastJobs.Persistence;
public enum AfterActionExecutionMode
{
    /// <summary>
    /// The after-action chain runs on every instance completion.
    /// </summary>
    RunPerInstance = 0,

    /// <summary>
    /// The after-action chain runs only on the final instance, determined by
    /// nextRun == null || nextRun > ExpiresAt, not wall-clock time.
    /// </summary>
    RunAfterFinalCompletion = 1
}
