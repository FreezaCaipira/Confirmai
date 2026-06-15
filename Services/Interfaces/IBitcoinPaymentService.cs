namespace Confirmai.Services.Interfaces;

public interface IBitcoinPaymentService
{
    Task<(string Address, string PaymentId)> GenerateAddressAsync(decimal amount, string? orderId = null);
    Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(decimal amount, string? orderId = null);
    Task<decimal> GetReceivedAmountAsync(string address);
    string Name { get; }
}
