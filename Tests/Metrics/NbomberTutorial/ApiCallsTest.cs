using System.Diagnostics;
using System.Net.Http.Json;
using FastJobs;
using Microsoft.Extensions.Logging;

public static class OffloadedCalls
{
    private static readonly HttpClient Client = new();

    public static async Task RunAsync()
    {
        var runId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();
        try
        {
            await Client.PostAsJsonAsync("https://httpbin.org/post",
                new { to = "user@example.com", subject = "Welcome", body = "Hello" });
            await Client.PostAsJsonAsync("https://jsonplaceholder.typicode.com/posts",
                new { pipeline = "build-main", branch = "main", trigger = "load-test" });
        }
        finally
        {
            sw.Stop();
            JobTimer.Record(nameof(OffloadedCalls), sw.Elapsed.TotalMilliseconds);
            Console.WriteLine($"{nameof(OffloadedCalls)} {runId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }
}