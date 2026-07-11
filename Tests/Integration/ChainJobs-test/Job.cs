using FastJobs;
public class ChainStepAJob : IBackGroundJob
{
    private readonly IChainExecutionRecorder _recorder;
    public ChainStepAJob(IChainExecutionRecorder recorder) => _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _recorder.RecordStart("A");
        await Task.Delay(200, cancellationToken);
        _recorder.RecordEnd("A");
    }
}

public class ChainStepBJob : IBackGroundJob
{
    private readonly IChainExecutionRecorder _recorder;
    public ChainStepBJob(IChainExecutionRecorder recorder) => _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _recorder.RecordStart("A");
        await Task.Delay(200, cancellationToken);
        _recorder.RecordEnd("A");
    }
}

public class ChainStepDJob : IBackGroundJob
{
    private readonly IChainExecutionRecorder _recorder;
    public ChainStepDJob(IChainExecutionRecorder recorder) => _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _recorder.RecordStart("A");
        await Task.Delay(200, cancellationToken);
        _recorder.RecordEnd("A");
    }
}

public class ChainStepCJob : IBackGroundJob
{
    private readonly IChainExecutionRecorder _recorder;
    public ChainStepCJob(IChainExecutionRecorder recorder) => _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _recorder.RecordStart("A");
        await Task.Delay(200, cancellationToken);
        _recorder.RecordEnd("A");
    }
}
// ChainStepBJob, ChainStepCJob, ChainStepDJob — identical shape, "B"/"C"/"D"