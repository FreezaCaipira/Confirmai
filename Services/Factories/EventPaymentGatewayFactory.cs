using Confirmai.Services.Payment;
namespace Confirmai.Services;

public sealed record EventPaymentGatewayOption(string Name, string DisplayName);

public sealed class EventPaymentGatewayFactory
{
    private readonly IEnumerable<IEventPaymentGateway> _gateways;
    private readonly GatewayService _gatewayService;

    public EventPaymentGatewayFactory(
        IEnumerable<IEventPaymentGateway> gateways,
        GatewayService gatewayService)
    {
        _gateways = gateways;
        _gatewayService = gatewayService;
    }

    public async Task<IReadOnlyList<EventPaymentGatewayOption>> GetAvailableAsync()
    {
        var enabledNames = await _gatewayService.GetEnabledGatewayNamesAsync();

        return _gateways
            .Where(g => g.IsAvailable && enabledNames.Contains(g.Name))
            .Select(g => new EventPaymentGatewayOption(g.Name, g.DisplayName))
            .OrderBy(g => g.Name)
            .ToList();
    }

    public async Task<IEventPaymentGateway?> GetGatewayAsync(string name)
    {
        var enabledNames = await _gatewayService.GetEnabledGatewayNamesAsync();

        return _gateways.FirstOrDefault(g =>
            g.IsAvailable &&
            enabledNames.Contains(g.Name) &&
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEventPaymentGateway?> GetDefaultAsync()
    {
        var options = await GetAvailableAsync();
        if (options.Count == 0)
            return null;

        return await GetGatewayAsync(options[0].Name);
    }

    public IEventPaymentGateway? GetByNameIgnoringToggle(string name)
    {
        return _gateways.FirstOrDefault(g =>
            g.IsAvailable &&
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IEventPaymentGateway> GetAllAvailableIgnoringToggle()
    {
        return _gateways.Where(g => g.IsAvailable).ToList();
    }

    public HashSet<string> GetConfiguredNames()
    {
        return _gateways
            .Where(g => g.IsAvailable)
            .Select(g => g.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
