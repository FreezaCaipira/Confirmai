using NBitcoin;
using System.Net.Http.Json;
using System.Text.Json;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Confirmai.Services
{
    public class TestnetBitcoinPaymentService : IBitcoinPaymentService
    {
        public string Name => "Testnet";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PaymentEventBus? _eventBus;

        public TestnetBitcoinPaymentService(IHttpClientFactory httpClientFactory, PaymentEventBus? eventBus = null)
        {
            _httpClientFactory = httpClientFactory;
            _eventBus = eventBus;
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
            catch
            {
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
                // Garante que a order existe
                if (!db.Orders.Any(o => o.PaymentId == payment.Id))
                {
                    await CreateOrderAsync(db, payment, log);
                }
                return true;
            }

            // Aqui você pode adicionar lógica para checar na blockchain se quiser
            payment.IsPaid = true;
            payment.PaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await log.LogAsync($"[Testnet] Pagamento marcado como pago para paymentId={paymentId}", source: "Testnet", level: "Info");

            // Cria order se não existir
            if (!db.Orders.Any(o => o.PaymentId == payment.Id))
            {
                await CreateOrderAsync(db, payment, log);
            }

            return true;
        }

        private async Task CreateOrderAsync(AppDbContext db, PaymentRecord payment, LogService log)
        {
            var buyerId = payment.UserId;
            var sellerId = payment.Product?.UserId;
            var participantDeleted = string.IsNullOrEmpty(buyerId) || string.IsNullOrEmpty(sellerId);

            if (participantDeleted)
            {
                await log.LogAsync($"[Testnet] Participante removido ao criar pedido para paymentId={payment.Id}. Pedido criado em AguardandoRevisaoAdm.", source: "Testnet", level: "Warning");
            }

            var order = new OrderModel
            {
                ServerId = payment.ServerId,
                BuyerId = string.IsNullOrEmpty(buyerId) ? null : buyerId,
                SellerId = string.IsNullOrEmpty(sellerId) ? null : sellerId,
                ProductId = payment.ProductId,
                Amount = payment.Amount,
                IsPaid = true,
                PaymentId = payment.Id,
                DeliveryAgentId = payment.DeliveryAgentId,
                EstimatedDeliveryDays = payment.EstimatedDeliveryDays,
                UseSiteIntermediary = payment.UseSiteIntermediary,
                ItemOfferId = payment.ItemOfferId,
                Status = participantDeleted ? PaymentStatus.AguardandoRevisaoAdm
                       : (payment.ServerId.HasValue ? PaymentStatus.AguardandoEntregaInGame : PaymentStatus.AguardandoEntrega),
                CreatedAt = DateTime.UtcNow
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            if (participantDeleted)
            {
                _eventBus?.NotifyOrderEnteredReview(order.Id);
            }

            db.OrderMessages.Add(new OrderMessage
            {
                OrderId = order.Id,
                UserId = null,
                UserRole = "admin",
                Text = $"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.",
                CreatedAt = DateTime.UtcNow
            });

            await CreateInitialMailboxConversationAsync(db, order);

            payment.OrderId = order.Id;
            db.Payments.Update(payment);
            await db.SaveChangesAsync();

            await log.LogAsync($"[Testnet] Order criada para paymentId={payment.PaymentId}, orderId={order.Id}", source: "Testnet", level: "Info");
        }

        private static async Task CreateInitialMailboxConversationAsync(AppDbContext db, OrderModel order)
        {
            var subject = $"Pedido {order.Id}";
            var body = $"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.";

            var adminRoleId = await db.Roles
                .Where(r => r.NormalizedName == "ADMIN")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var adminUserIds = string.IsNullOrWhiteSpace(adminRoleId)
                ? new List<string>()
                : await db.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

            var primaryAdminId = adminUserIds.FirstOrDefault();
            var senderId = !string.IsNullOrWhiteSpace(primaryAdminId) ? primaryAdminId : order.BuyerId;

            if (string.IsNullOrWhiteSpace(senderId))
                return;

            var recipients = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(order.BuyerId))
                recipients.Add(order.BuyerId);
            if (!string.IsNullOrWhiteSpace(order.SellerId))
                recipients.Add(order.SellerId);
            foreach (var adminId in adminUserIds)
                recipients.Add(adminId);

            recipients.Remove(senderId);

            var now = DateTime.UtcNow;
            foreach (var recipientId in recipients)
            {
                db.UserMailboxMessages.Add(new UserMailboxMessage
                {
                    SenderUserId = senderId,
                    RecipientUserId = recipientId,
                    Subject = subject,
                    Body = body,
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }
    }
}

