namespace Confirmai.Services.Core;

public sealed class DebounceDispatcher : IDisposable
{
    private CancellationTokenSource? _cts;

    public async Task DebounceAsync(TimeSpan delay, Func<Task> action)
    {
        var nextCts = new CancellationTokenSource();
        var previousCts = Interlocked.Exchange(ref _cts, nextCts);
        TryCancelAndDispose(previousCts);
        var token = nextCts.Token;

        try
        {
            await Task.Delay(delay, token);
            if (!token.IsCancellationRequested)
            {
                await action();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        TryCancelAndDispose(cts);
    }

    private static void TryCancelAndDispose(CancellationTokenSource? cts)
    {
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        cts.Dispose();
    }
}

