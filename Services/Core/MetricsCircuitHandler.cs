using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Confirmai.Services.Core;

/// <summary>
/// C32 Fase C — counts active Blazor circuits and reconnects.
/// A reconnect is a circuit that goes connection-down and then comes back up;
/// the first OnConnectionUp after open is the normal initial connect.
/// </summary>
public sealed class MetricsCircuitHandler : CircuitHandler
{
    private readonly OperationalMetrics _metrics;

    // Circuits that lost their connection at least once. A subsequent
    // OnConnectionUp means the client reconnected rather than connected.
    private static readonly ConcurrentDictionary<string, byte> DisconnectedCircuits = new();

    public MetricsCircuitHandler(OperationalMetrics metrics)
    {
        _metrics = metrics;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _metrics.CircuitOpened();
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        DisconnectedCircuits.TryRemove(circuit.Id, out _);
        _metrics.CircuitClosed();
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        DisconnectedCircuits[circuit.Id] = 1;
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (DisconnectedCircuits.ContainsKey(circuit.Id))
        {
            _metrics.CircuitReconnected();
        }

        return Task.CompletedTask;
    }
}
