using Confirmai.Services.Interfaces;

namespace Confirmai.Services.Factories;

public class BitcoinPaymentFactory
{
    private readonly IEnumerable<IBitcoinPaymentService> _services;

    public BitcoinPaymentFactory(IEnumerable<IBitcoinPaymentService> services)
    {
        _services = services;
    }

    public IBitcoinPaymentService GetService(string name)
    {
        return _services.FirstOrDefault(s => s.Name == name)
            ?? _services.First();
    }

    public IEnumerable<string> GetAvailableMethods() => _services.Select(s => s.Name);
}
