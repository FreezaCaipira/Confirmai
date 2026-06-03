using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class DebounceDispatcherTests
{
    [Fact]
    public async Task DebounceAsync_WhenTriggeredMultipleTimes_ExecutesOnlyLastAction()
    {
        using var dispatcher = new DebounceDispatcher();
        var firstCalls = 0;
        var secondCalls = 0;

        var firstTask = dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(80),
            () =>
            {
                Interlocked.Increment(ref firstCalls);
                return Task.CompletedTask;
            });

        var secondTask = dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(80),
            () =>
            {
                Interlocked.Increment(ref secondCalls);
                return Task.CompletedTask;
            });

        await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(0, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public async Task DebounceAsync_WhenDisposed_CancelsPendingAction()
    {
        using var dispatcher = new DebounceDispatcher();
        var executed = false;

        var task = dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(200),
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        // Dispose immediately to avoid timing-dependent flakes under CI/load.
        dispatcher.Dispose();
        await task;

        Assert.False(executed);
    }
}


