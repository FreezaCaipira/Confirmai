using Confirmai.Services.Payment;
using Confirmai.Services.Core;
using NBitcoin;
using System.Text.Json;
using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Utility
{
    public class TestnetBitcoinPaymentService : IBitcoinPaymentService
    {
        public string Name => "Testnet";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PaymentEventBus? _eventBus;
        private readonly ILogger<TestnetBitcoinPaymentService> _logger;

        public TestnetBitcoinPaymentService(IHttpClientFactory httpClientFactory, PaymentEventBus? eventBus = null, ILogger<TestnetBitcoinPaymentService>? logger = null)
        {
            _httpClientFactory = httpClientFactory;
            _eventBus = eventBus;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TestnetBitcoinPaymentService>.Instance;
        }

        public Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(decimal amount, string? orderId = null)
        {
            var network = Network.TestNet;
            var key = new Key();
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
            var privateKey = key.GetWif(network).ToString();
            var paymentId = Guid.NewGuid().ToString();

            return Task.FromResult((address, paymentId, privateKey));
        }

        public Task<(string Address, string PaymentId)> GenerateAddressAsync(decimal amount, string? orderId = null)
        {
            var network = Network.TestNet;
            var key = new Key();
            var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
            var paymentId = Guid.NewGuid().ToString();

            return Task.FromResult((address, paymentId));
        }

        public async Task<decimal> GetReceivedAmountAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return 0m;

            var normalizedAddress = address.Trim();

            try
            {
                var receivedFromBlockCypher = await GetReceivedFromBlockCypherAsync(normalizedAddress);
                if (receivedFromBlockCypher > 0m)
                    return receivedFromBlockCypher;

                return await GetReceivedFromBlockstreamAsync(normalizedAddress);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Testnet: falha ao consultar saldo do endereço {Address}", normalizedAddress);
                return 0m;
            }
        }

        private async Task<decimal> GetReceivedFromBlockCypherAsync(string address)
        {
            var http = _httpClientFactory.CreateClient();
            var url = $"https://api.blockcypher.com/v1/btc/test3/addrs/{address}/balance";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("total_received", out var totalReceivedProp))
                return 0m;

            var received = totalReceivedProp.GetInt64();
            return received / 100_000_000m;
        }

        private async Task<decimal> GetReceivedFromBlockstreamAsync(string address)
        {
            var http = _httpClientFactory.CreateClient();
            var url = $"https://blockstream.info/testnet/api/address/{address}";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("chain_stats", out var chainStats))
                return 0m;

            var funded = chainStats.TryGetProperty("funded_txo_sum", out var fundedProp)
                ? fundedProp.GetInt64()
                : 0L;

            var spent = chainStats.TryGetProperty("spent_txo_sum", out var spentProp)
                ? spentProp.GetInt64()
                : 0L;

            var received = Math.Max(0L, funded - spent);

            return received / 100_000_000m;
        }

        public async Task<bool> CheckAndMarkPaymentAsync(AppDbContext db, LogService log, string paymentId)
        {
            var payment = db.Payments.Include(p => p.Product).FirstOrDefault(p => p.PaymentId == paymentId);
            if (payment == null)
            {
                await log.LogAsync($"[Testnet] Pagamento não encontrado para paymentId={paymentId}", source: "Testnet", level: "Warning");
                return false;
            }

            if (payment.IsPaid)
            {
                await log.LogAsync($"[Testnet] Pagamento já está marcado como pago para paymentId={paymentId}", source: "Testnet", level: "Info");
                return true;
            }

            payment.IsPaid = true;
            payment.PaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await log.LogAsync($"[Testnet] Pagamento marcado como pago para paymentId={paymentId}", source: "Testnet", level: "Info");

            if (payment.UserId != null)
                _eventBus?.NotifyPaymentConfirmed(payment.UserId, payment.PaymentId ?? paymentId);

            return true;
        }
    }
}


