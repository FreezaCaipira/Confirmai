using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Utility;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace Confirmai.Services.Payment;

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

        if (!string.IsNullOrEmpty(dbPayment.UserId))
        {
            await _hubContext.Clients.User(dbPayment.UserId).SendAsync("PaymentConfirmed", dbPayment.PaymentId);
            _eventBus.NotifyPaymentConfirmed(dbPayment.UserId, dbPayment.PaymentId ?? string.Empty);
        }

        return (true, false, received);
    }
}
