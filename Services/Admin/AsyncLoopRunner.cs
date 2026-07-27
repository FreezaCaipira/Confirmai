namespace Confirmai.Services.Admin;

public sealed class AsyncLoopRunner : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public bool IsRunning => _loopTask is not null && !_loopTask.IsCompleted;

    public void Start(TimeSpan interval, Func<CancellationToken, Task> callback)
    {
        if (_cts is not null)
            return;

        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_cts.Token, interval, callback);
    }

    private static async Task RunLoopAsync(CancellationToken cancellationToken, TimeSpan interval, Func<CancellationToken, Task> callback)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken);
                await callback(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_cts is not null)
        {
            _cts.Cancel();

            if (_loopTask is not null)
            {
                try
                {
                    await _loopTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cts.Dispose();
            _cts = null;
        }
    }
}
