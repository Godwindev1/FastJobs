using System.Net.Http.Json;
using FastJobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
public class ImageCompressJob(IHttpClientFactory http, IJobContext ctx, ILogger<ImageCompressJob> logger) : IBackGroundJob
{
    public Task ExecuteAsync(CancellationToken ct) => JobTimer.RunAsync(nameof(ImageCompressJob), ctx.CurrentJob?.Id, logger, async () =>
    {
        var bytes = await http.CreateClient().GetByteArrayAsync("https://picsum.photos/1600/1200", ct);
        using var image = Image.Load(bytes);
        using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 60 }, ct);
        logger.LogInformation("Compressed {Before} -> {After} bytes", bytes.Length, output.Length);
    });
}