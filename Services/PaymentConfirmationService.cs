using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Hubs;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace Confirmai.Services
{
    public class PaymentConfirmationService
    {
        private readonly AppDbContext _db;
        private readonly BitcoinPaymentFactory _paymentFactory;
        private readonly LogService _logService;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly PaymentEventBus _eventBus;

        public PaymentConfirmationService(
            AppDbContext db,
            BitcoinPaymentFactory paymentFactory,
            LogService logService,
            IHubContext<PaymentHub> hubContext,
            PaymentEventBus eventBus)
        {
            _db = db;
            _paymentFactory = paymentFactory;
            _logService = logService;
            _hubContext = hubContext;
            _eventBus = eventBus;
        }

        public async Task<(bool Confirmed, bool AlreadyPaid, decimal ReceivedAmount)> ConfirmAsync(PaymentRecord payment)
        {
            var dbPayment = await _db.Payments
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == payment.Id);

            if (dbPayment == null)
                return (false, false, 0m);

            var paymentMethod = dbPayment.PaymentMethod ?? "Testnet";
            var service = _paymentFactory.GetService(paymentMethod);

            if (dbPayment.IsPaid)
            {
                if (service is TestnetBitcoinPaymentService testnetPaidService && !string.IsNullOrEmpty(dbPayment.PaymentId))
                {
                    await testnetPaidService.CheckAndMarkPaymentAsync(_db, _logService, dbPayment.PaymentId);
                }

                return (true, true, dbPayment.Amount);
            }

            var parameter = paymentMethod == "Testnet"
                ? dbPayment.Address
                : dbPayment.PaymentId ?? dbPayment.Address;

            var received = await service.GetReceivedAmountAsync(parameter);
            if (received < dbPayment.Amount)
                return (false, false, received);

            // S-4: Use optimistic concurrency to prevent race conditions
            try
            {
                dbPayment.IsPaid = true;
                dbPayment.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return (true, true, received);
            }

            if (service is TestnetBitcoinPaymentService testnetService && !string.IsNullOrEmpty(dbPayment.PaymentId))
            {
                await testnetService.CheckAndMarkPaymentAsync(_db, _logService, dbPayment.PaymentId);
            }

            await _logService.LogAsync(
                $"Pagamento confirmado para paymentId={dbPayment.PaymentId} via {paymentMethod}.",
                source: "Payment",
                level: "Info",
                userId: dbPayment.UserId
            );

            return (true, false, received);
        }

        /// <summary>
        /// Called by the seller to manually confirm a PIX payment. Marks payment as paid and creates the order.
        /// Returns (success, errorMessage).
        /// </summary>
        public async Task<(bool Success, string? Error)> ConfirmPixReceiptAsync(int paymentId, string sellerUserId)
        {
            var payment = await _db.Payments
                .Include(p => p.Product)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return (false, "Pagamento não encontrado.");

            if (payment.IsPaid)
                return (false, "Este pagamento já foi confirmado.");

            // Only the seller or product owner may confirm
            var isAuthorized = string.Equals(payment.SellerId, sellerUserId, StringComparison.Ordinal)
                || string.Equals(payment.Product?.UserId, sellerUserId, StringComparison.Ordinal);

            if (!isAuthorized)
                return (false, "Você não tem permissão para confirmar este pagamento.");

            // S-4: Use optimistic concurrency to prevent race conditions
            try
            {
                payment.IsPaid = true;
                payment.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, "Este pagamento já foi confirmado por outra operação.");
            }

            // Create order
            var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
            if (existingOrder == null)
            {
                var buyerId = payment.UserId;
                var sellerId = payment.SellerId ?? payment.Product?.UserId;
                var participantDeleted = string.IsNullOrEmpty(buyerId) || string.IsNullOrEmpty(sellerId);

                var order = new OrderModel
                {
                    BuyerId = string.IsNullOrEmpty(buyerId) ? null : buyerId,
                    SellerId = string.IsNullOrEmpty(sellerId) ? null : sellerId,
                    ProductId = payment.ProductId,
                    Amount = payment.Amount,
                    IsPaid = true,
                    PaymentId = payment.Id,
                    EstimatedDeliveryDays = payment.EstimatedDeliveryDays,
                    UseSiteIntermediary = payment.UseSiteIntermediary,
                    Status = participantDeleted ? PaymentStatus.AguardandoRevisaoAdm : PaymentStatus.AguardandoEntrega,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                var chatText = participantDeleted
                    ? $"Pedido {order.Id} criado via PIX, mas um participante foi removido. Pedido bloqueado aguardando revisão administrativa."
                    : $"Pagamento PIX confirmado pelo vendedor. Pedido {order.Id} iniciado.";

                _db.OrderMessages.Add(new OrderMessage
                {
                    OrderId = order.Id,
                    UserId = null,
                    UserRole = "admin",
                    Text = chatText,
                    CreatedAt = DateTime.UtcNow
                });

                payment.OrderId = order.Id;
                _db.Payments.Update(payment);
                await _db.SaveChangesAsync();

                if (participantDeleted)
                {
                    _eventBus.NotifyOrderEnteredReview(order.Id);
                }
            }

            await _logService.LogAsync(
                $"Pagamento PIX {paymentId} confirmado manualmente pelo vendedor {sellerUserId}.",
                source: "Payment",
                level: "Info",
                userId: payment.UserId
            );

            // Notify buyer via SignalR + in-process event bus
            if (!string.IsNullOrEmpty(payment.UserId))
            {
                await _hubContext.Clients.User(payment.UserId).SendAsync("PaymentConfirmed", payment.PaymentId);
                _eventBus.NotifyPaymentConfirmed(payment.UserId, payment.PaymentId ?? string.Empty);
            }

            return (true, null);
        }
    }
}

