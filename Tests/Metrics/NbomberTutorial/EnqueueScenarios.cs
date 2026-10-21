using FastJobs;
using NBomber.Contracts;
using NBomber.CSharp;

public static class EnqueueScenarios
{
    // Placeholders: replace once targets are set
    const int Rate = 50;
    static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);

    static ScenarioProps Build(string name, Func<Task> enqueue) =>
        Scenario.Create(name, async context =>
        {
            await enqueue();
            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(Simulation.Inject(Rate, Interval, Duration));

    public static ScenarioProps FireAndForget() =>
        Build("enqueue_offloaded_calls", () =>
            FastJobServer.EnqueueJob(() => OffloadedCalls.RunAsync()).Start());

    public static ScenarioProps InvoicePdf() =>
        Build("enqueue_invoice_pdf", () =>
            FastJobServer.EnqueueJob<InvoicePdfJob>().Start());

    public static ScenarioProps ImageCompress() =>
        Build("enqueue_image_compress", () =>
            FastJobServer.EnqueueJob<ImageCompressJob>().Start());
}

