using System.Net.Http.Json;
using FastJobs;
using Microsoft.Extensions.Logging;
using SkiaSharp;

public class ImageCompressJob(IHttpClientFactory http, IJobContext ctx, ILogger<ImageCompressJob> logger) : IBackGroundJob
{
    public Task ExecuteAsync(CancellationToken ct) => JobTimer.RunAsync(nameof(ImageCompressJob), ctx.CurrentJob?.Id, logger, async () =>
    {
        var bytes = await http.CreateClient().GetByteArrayAsync("https://picsum.photos/1600/1200", ct);

        using var bitmap = SKBitmap.Decode(bytes);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 60);

        logger.LogInformation("Compressed {Before} -> {After} bytes", bytes.Length, data.Size);
    });
}