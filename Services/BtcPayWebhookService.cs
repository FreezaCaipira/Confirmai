using System.Text.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Hubs;
using Confirmai.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Confirmai.Services
{
    public class BtcPayWebhookService
    {
        private const int DefaultMaxWebhookBodyBytes = 64 * 1024;
        private const int MaxWebhookAgeSeconds = 300; // 5 minutes
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _processedDeliveries = new();
        private static DateTimeOffset _lastCleanup = DateTimeOffset.UtcNow;
        private readonly AppDbContext _db;
        private readonly LogService _log;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly PaymentEventBus _eventBus;

        public BtcPayWebhookService(
            AppDbContext db,
            LogService log,
            IConfiguration configuration,
            IHubContext<PaymentHub> hubContext,
            PaymentEventBus eventBus)
        {
            _db = db;
            _log = log;
            _configuration = configuration;
            _hubContext = hubContext;
            _eventBus = eventBus;
        }

        public async Task<IResult> HandleAsync(HttpContext context)
        {
            await _log.LogAsync("Webhook chamado.", source: AdminAuditSources.Webhook, level: "Info");

            var expectedSecret = _configuration["BtcPay:WebhookSecret"];
            var receivedSecret = GetSingleWebhookSecretHeader(context.Request.Headers);

            if (!IsValidWebhookSecret(expectedSecret, receivedSecret))
            {
                await _log.LogAsync(
                    $"Tentativa de acesso negada ao webhook. Header recebido: '{receivedSecret ?? "null"}'. IP: {context.Connection.RemoteIpAddress}",
                    source: AdminAuditSources.Webhook,
                    level: "Warning"
                );
                return Results.Unauthorized();
            }

            var maxWebhookBodyBytes = GetMaxWebhookBodyBytes();

            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > maxWebhookBodyBytes)
            {
                await _log.LogAsync(
                    $"Payload do webhook excede limite permitido ({context.Request.ContentLength.Value} bytes). Limite: {maxWebhookBodyBytes} bytes.",
                    source: AdminAuditSources.Webhook,
                    level: "Warning"
                );
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            if (Encoding.UTF8.GetByteCount(body) > maxWebhookBodyBytes)
            {
                await _log.LogAsync(
                    $"Payload do webhook excede limite permitido após leitura do corpo. Limite: {maxWebhookBodyBytes} bytes.",
                    source: AdminAuditSources.Webhook,
                    level: "Warning"
                );
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            await _log.LogAsync($"Webhook chamado. Body recebido: {body}", source: AdminAuditSources.Webhook, level: "Info");

            if (!TryParseWebhookPayload(body, out var invoiceId, out var status, out var deliveryId, out var webhookTimestamp, out var logMessage, out var responseMessage))
            {
                await _log.LogAsync(logMessage, source: AdminAuditSources.Webhook, level: "Warning");
                return Results.BadRequest(responseMessage);
            }

            // Replay protection: reject webhooks older than 5 minutes
            if (webhookTimestamp > 0)
            {
                var deliveryTime = DateTimeOffset.FromUnixTimeSeconds(webhookTimestamp);
                var age = DateTimeOffset.UtcNow - deliveryTime;
                if (age.TotalSeconds > MaxWebhookAgeSeconds || age.TotalSeconds < -60)
                {
                    await _log.LogAsync($"Webhook rejeitado por timestamp expirado: {webhookTimestamp} (idade: {age.TotalSeconds:F0}s)", source: AdminAuditSources.Webhook, level: "Warning");
                    return Results.Ok();
                }
            }

            // Replay protection: reject duplicate deliveryIds
            if (!string.IsNullOrEmpty(deliveryId))
            {
                if (!_processedDeliveries.TryAdd(deliveryId, DateTimeOffset.UtcNow))
                {
                    await _log.LogAsync($"Webhook delivery duplicado ignorado: {deliveryId}", source: AdminAuditSources.Webhook, level: "Info");
                    return Results.Ok();
                }
                CleanupOldDeliveries();
            }

            await _log.LogAsync($"Webhook recebido: status={status}, invoiceId={invoiceId}, deliveryId={deliveryId}", source: AdminAuditSources.Webhook, level: "Info");

            if (status != "InvoiceSettled")
                return Results.Ok();

            var payment = await _db.Payments.Include(p => p.Product).FirstOrDefaultAsync(p => p.PaymentId == invoiceId);

            if (payment == null)
            {
                await _log.LogAsync($"Pagamento não encontrado para invoiceId={invoiceId}", source: AdminAuditSources.Webhook, level: "Warning");
                return Results.Ok();
            }

            if (payment.IsPaid)
            {
                await _log.LogAsync($"Pagamento já está marcado como pago para invoiceId={invoiceId}", source: AdminAuditSources.Webhook, level: "Info");
                await EnsureOrderAsync(payment, invoiceId);
                return Results.Ok();
            }

            // S-4: Use optimistic concurrency to prevent race conditions
            try
            {
                payment.IsPaid = true;
                payment.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await _log.LogAsync($"Concurrency conflict para invoiceId={invoiceId} — outro handler ja processou.", source: AdminAuditSources.Webhook, level: "Info");
                return Results.Ok();
            }

            await _log.AuditAsync(
                AuditEvents.PaymentConfirmed,
                AuditEntities.Payment,
                payment.Id.ToString(),
                $"Pagamento confirmado via webhook BtcPay (invoice {invoiceId}).",
                actorUserId: payment.UserId,
                source: AdminAuditSources.Payments,
                metadata: new { PaymentId = payment.Id, InvoiceId = invoiceId, payment.Amount, payment.UserId, payment.ProductId, payment.ServerId });

            await _log.LogAsync(
                $"Preparando para criar pedido: paymentId={payment.Id}, userId={payment.UserId}, productId={payment.ProductId}, productUserId={payment.Product?.UserId}",
                source: AdminAuditSources.Webhook,
                level: "Info"
            );

            await EnsureOrderAsync(payment, invoiceId);

            if (!string.IsNullOrEmpty(payment.UserId))
            {
                await _hubContext.Clients.User(payment.UserId).SendAsync("PaymentConfirmed", payment.PaymentId);
                _eventBus.NotifyPaymentConfirmed(payment.UserId, payment.PaymentId ?? string.Empty);
            }

            return Results.Ok();
        }

        private static bool IsValidWebhookSecret(string? expectedSecret, string? receivedSecret)
        {
            if (string.IsNullOrEmpty(expectedSecret) || string.IsNullOrEmpty(receivedSecret))
                return false;

            var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
            var receivedBytes = Encoding.UTF8.GetBytes(receivedSecret);

            if (expectedBytes.Length != receivedBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
        }

        private static string? GetSingleWebhookSecretHeader(IHeaderDictionary headers)
        {
            if (!headers.TryGetValue("X-BTCPay-Secret", out StringValues values))
                return null;

            if (values.Count != 1)
                return null;

            return values[0];
        }

        private static bool TryParseWebhookPayload(
            string body,
            out string invoiceId,
            out string status,
            out string deliveryId,
            out long timestamp,
            out string logMessage,
            out string responseMessage)
        {
            invoiceId = string.Empty;
            status = string.Empty;
            deliveryId = string.Empty;
            timestamp = 0;
            logMessage = string.Empty;
            responseMessage = string.Empty;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch (JsonException ex)
            {
                logMessage = $"Payload JSON inválido no webhook: {ex.Message}";
                responseMessage = "Payload inválido.";
                return false;
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("invoiceId", out var invoiceIdProp) ||
                    !doc.RootElement.TryGetProperty("type", out var typeProp))
                {
                    logMessage = "Payload do webhook sem campos obrigatórios (invoiceId/type).";
                    responseMessage = "Campos obrigatórios ausentes.";
                    return false;
                }

                if (invoiceIdProp.ValueKind != JsonValueKind.String || typeProp.ValueKind != JsonValueKind.String)
                {
                    logMessage = "Payload do webhook com tipos inválidos para invoiceId/type.";
                    responseMessage = "Tipos de campos inválidos.";
                    return false;
                }

                invoiceId = invoiceIdProp.GetString() ?? string.Empty;
                status = typeProp.GetString() ?? string.Empty;

                if (doc.RootElement.TryGetProperty("deliveryId", out var deliveryIdProp) &&
                    deliveryIdProp.ValueKind == JsonValueKind.String)
                {
                    deliveryId = deliveryIdProp.GetString() ?? string.Empty;
                }

                if (doc.RootElement.TryGetProperty("timestamp", out var timestampProp) &&
                    timestampProp.ValueKind == JsonValueKind.Number)
                {
                    timestamp = timestampProp.GetInt64();
                }

                if (string.IsNullOrWhiteSpace(invoiceId) || string.IsNullOrWhiteSpace(status))
                {
                    logMessage = "Payload do webhook com invoiceId/type vazios.";
                    responseMessage = "Campos obrigatórios inválidos.";
                    return false;
                }
            }

            return true;
        }

        private int GetMaxWebhookBodyBytes()
        {
            var configuredValue = _configuration["BtcPay:WebhookMaxBodyBytes"];
            if (int.TryParse(configuredValue, out var parsed) && parsed > 0)
                return parsed;

            return DefaultMaxWebhookBodyBytes;
        }

        private static void CleanupOldDeliveries()
        {
            if ((DateTimeOffset.UtcNow - _lastCleanup).TotalMinutes < 10)
                return;

            _lastCleanup = DateTimeOffset.UtcNow;
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-MaxWebhookAgeSeconds * 2);
            foreach (var kvp in _processedDeliveries)
            {
                if (kvp.Value < cutoff)
                    _processedDeliveries.TryRemove(kvp.Key, out _);
            }
        }

        private async Task EnsureOrderAsync(PaymentRecord payment, string invoiceId)
        {
            var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
            if (existingOrder != null)
            {
                if (payment.OrderId != existingOrder.Id)
                {
                    payment.OrderId = existingOrder.Id;
                    _db.Payments.Update(payment);
                    await _db.SaveChangesAsync();
                }
                return;
            }

            try
            {
                var buyerId = payment.UserId;
                var sellerId = payment.Product?.UserId;
                var participantDeleted = string.IsNullOrEmpty(buyerId) || string.IsNullOrEmpty(sellerId);

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

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                var chatText = participantDeleted
                    ? $"Pedido {order.Id} criado, mas um participante foi removido. Pedido bloqueado aguardando revisão administrativa."
                    : $"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.";

                _db.OrderMessages.Add(new OrderMessage
                {
                    OrderId = order.Id,
                    UserId = null,
                    UserRole = "admin",
                    Text = chatText,
                    CreatedAt = DateTime.UtcNow
                });

                await CreateInitialMailboxConversationAsync(order);

                payment.OrderId = order.Id;
                _db.Payments.Update(payment);
                await _db.SaveChangesAsync();

                await _log.AuditAsync(
                    AuditEvents.OrderCreated,
                    AuditEntities.Order,
                    order.Id.ToString(),
                    $"Pedido {order.Id} criado a partir do invoice {invoiceId}.",
                    actorUserId: payment.UserId,
                    source: AdminAuditSources.Orders,
                    metadata: new { OrderId = order.Id, PaymentId = payment.Id, InvoiceId = invoiceId, order.BuyerId, order.SellerId, order.ProductId, order.Amount, order.Status, ParticipantDeleted = participantDeleted });

                if (participantDeleted)
                {
                    _eventBus.NotifyOrderEnteredReview(order.Id);
                }
            }
            catch (Exception ex)
            {
                await _log.LogAsync($"Erro ao criar pedido: {ex.Message}", source: AdminAuditSources.Webhook, level: "Error");
            }
        }

        private async Task CreateInitialMailboxConversationAsync(OrderModel order)
        {
            var subject = $"Pedido {order.Id}";
            var body = $"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.";

            var adminRoleId = await _db.Roles
                .Where(r => r.NormalizedName == "ADMIN")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var adminUserIds = string.IsNullOrWhiteSpace(adminRoleId)
                ? new List<string>()
                : await _db.UserRoles
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
                _db.UserMailboxMessages.Add(new UserMailboxMessage
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

