using Microsoft.AspNetCore.Components;

namespace FastJobs.Dashboard;

/// <summary>
/// Base component that loads its data on init and then reloads it periodically
/// while the interactive circuit is alive. Ticks never overlap and a failed
/// refresh is swallowed so the next tick can retry.
/// </summary>
public abstract class AutoRefreshComponent : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    protected abstract TimeSpan RefreshInterval { get; }

    protected abstract Task LoadData();

    protected override Task OnInitializedAsync() => LoadData();

    protected override void OnAfterRender(bool firstRender)
    {
        // OnAfterRender only runs once the component is interactive, never during static/prerender.
        if (firstRender)
            _ = RefreshLoop(_cts.Token);
    }

    private async Task RefreshLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(RefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    await InvokeAsync(async () =>
                    {
                        await LoadData();
                        StateHasChanged();
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Keep the loop alive; the next tick retries.
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public virtual void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
