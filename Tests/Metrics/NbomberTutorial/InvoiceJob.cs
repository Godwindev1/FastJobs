using System.Net.Http.Json;
using FastJobs;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

public class InvoicePdfJob(IJobContext ctx, ILogger<InvoicePdfJob> logger) : IBackGroundJob
{
    public Task ExecuteAsync(CancellationToken ct) => JobTimer.RunAsync(nameof(InvoicePdfJob), ctx.CurrentJob?.Id, logger, () =>
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "runtime", "invoices");
        Directory.CreateDirectory(dir);

        var doc = new PdfDocument();
        var gfx = XGraphics.FromPdfPage(doc.AddPage());
        var font = new XFont("Arial", 12);
        gfx.DrawString($"Invoice {ctx.CurrentJob?.Id}", font, XBrushes.Black, 40, 60);
        gfx.DrawString("Item: Service   Qty: 1   Total: 100.00", font, XBrushes.Black, 40, 90);
        doc.Save(Path.Combine(dir, $"invoice-{ctx.CurrentJob?.Id}.pdf"));
        return Task.CompletedTask;
    });
}