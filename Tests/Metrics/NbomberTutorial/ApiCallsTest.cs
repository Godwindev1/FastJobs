using System.Net.Http.Json;
using FastJobs;
using Microsoft.Extensions.Logging;

public class OffloadedCalls(IHttpClientFactory http, IJobContext ctx, ILogger<OffloadedCalls> logger)
{
    public Task RunAsync() => JobTimer.RunAsync(nameof(OffloadedCalls), ctx.CurrentJob?.Id, logger, async () =>
    {
        var client = http.CreateClient();
        await client.PostAsJsonAsync("https://httpbin.org/post",
            new { to = "user@example.com", subject = "Welcome", body = "Hello" });          // email simulation
        await client.PostAsJsonAsync("https://jsonplaceholder.typicode.com/posts",
            new { pipeline = "build-main", branch = "main", trigger = "load-test" });       // CI/CD trigger simulation
    });
}