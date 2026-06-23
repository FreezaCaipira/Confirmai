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

    [Fact]
    public async Task DebounceAsync_ExecutesAction_WhenSingleCall()
    {
        using var dispatcher = new DebounceDispatcher();
        var executed = false;

        await dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(10),
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        Assert.True(executed);
    }

    [Fact]
    public async Task DebounceAsync_ExecutesAction_WhenDelayElapses()
    {
        using var dispatcher = new DebounceDispatcher();
        var executed = false;

        var task = dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(50),
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        await task;

        Assert.True(executed);
    }

    [Fact]
    public async Task DebounceAsync_WithMultipleConcurrentCalls_ExecutesOnlyLast()
    {
        using var dispatcher = new DebounceDispatcher();
        var callCount = 0;
        var lastValue = 0;

        var tasks = Enumerable.Range(1, 5).Select(i =>
            dispatcher.DebounceAsync(
                TimeSpan.FromMilliseconds(50),
                () =>
                {
                    Interlocked.Increment(ref callCount);
                    lastValue = i;
                    return Task.CompletedTask;
                })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, callCount);
        Assert.Equal(5, lastValue);
    }

    [Fact]
    public async Task DebounceAsync_WithZeroDelay_ExecutesImmediately()
    {
        using var dispatcher = new DebounceDispatcher();
        var executed = false;

        await dispatcher.DebounceAsync(
            TimeSpan.Zero,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        Assert.True(executed);
    }

    [Fact]
    public async Task DebounceAsync_WhenActionThrows_PropagatesException()
    {
        using var dispatcher = new DebounceDispatcher();

        var task = dispatcher.DebounceAsync(
            TimeSpan.FromMilliseconds(10),
            () => throw new InvalidOperationException("Test exception"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoNotThrow()
    {
        var dispatcher = new DebounceDispatcher();

        dispatcher.Dispose();
        dispatcher.Dispose(); // Should not throw
    }

    [Fact]
    public void TryCancelAndDispose_WhenNull_DoesNotThrow()
    {
        var method = typeof(DebounceDispatcher).GetMethod("TryCancelAndDispose",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("TryCancelAndDispose method not found.");

        method.Invoke(null, new object[] { null });

        Assert.True(true); // If no exception is thrown, the test passes
    }

    [Fact]
    public void TryCancelAndDispose_WhenDisposedToken_DoesNotThrow()
    {
        var method = typeof(DebounceDispatcher).GetMethod("TryCancelAndDispose",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("TryCancelAndDispose method not found.");

        var cts = new CancellationTokenSource();
        cts.Dispose();

        method.Invoke(null, new object[] { cts });

        Assert.True(true); // If no exception is thrown, the test passes
    }
}


