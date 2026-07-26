using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Pages.Payment;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Crypto;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Utility;
using Confirmai.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Payment;

public sealed record GenerateAddressResult(
    string Address,
    string PaymentId,
    string? PrivateKey,
    PaymentRecord PaymentRecord);

public sealed class PaymentCommandService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly BitcoinPaymentFactory _paymentFactory;
    private readonly PaymentConfirmationService _confirmationService;
    private readonly LogService _logService;
    private readonly AdminSettingsService _adminSettingsService;

    public PaymentCommandService(
        IDbContextFactory<AppDbContext> dbFactory,
        BitcoinPaymentFactory paymentFactory,
        PaymentConfirmationService confirmationService,
        LogService logService,
        AdminSettingsService adminSettingsService)
    {
        _dbFactory = dbFactory;
        _paymentFactory = paymentFactory;
        _confirmationService = confirmationService;
        _logService = logService;
        _adminSettingsService = adminSettingsService;
    }

    public async Task<GenerateAddressResult?> GenerateBtcAddressAsync(
        int productId,
        decimal amount,
        string selectedMethod,
        string? sellerUserId,
        string? buyerUserId,
        string currency)
    {
        IBitcoinPaymentService? service = _paymentFactory.GetService(selectedMethod);
        string? privateKey = null;

        if (selectedMethod == "Testnet")
        {
            var (address, paymentId, privKey) = await ((TestnetBitcoinPaymentService)service)
                .GenerateAddressWithKeyAsync(amount, orderId: $"order-{productId}-{Guid.NewGuid()}");
            return new GenerateAddressResult(address, paymentId, privKey, BuildPaymentRecord(productId, buyerUserId, sellerUserId, currency, address, paymentId, amount, selectedMethod, privKey));
        }
        else
        {
            var (address, paymentId) = await service.GenerateAddressAsync(amount, $"order-{productId}-{Guid.NewGuid()}");
            return new GenerateAddressResult(address, paymentId, null, BuildPaymentRecord(productId, buyerUserId, sellerUserId, currency, address, paymentId, amount, selectedMethod, null));
        }
    }

    public async Task<GenerateAddressResult?> GeneratePixPaymentAsync(
        int productId,
        decimal amount,
        string selectedMethod,
        string? sellerUserId,
        string? buyerUserId,
        string currency,
        bool abacatePayEnabled,
        Product? product,
        bool useSiteIntermediary)
    {
        string address;
        string paymentId;

        if (abacatePayEnabled)
        {
            var orderId = $"order-{productId}-{Guid.NewGuid():N}";
            var pixService = _paymentFactory.GetService("Pix");
            var (pixAddress, pixPaymentId) = await pixService.GenerateAddressAsync(amount, orderId);
            address = pixAddress;
            paymentId = pixPaymentId;
        }
        else
        {
            var recipientPixKey = await ResolvePixRecipientKeyAsync(useSiteIntermediary, sellerUserId, product);
            if (string.IsNullOrWhiteSpace(recipientPixKey))
                return null;

            var merchantName = PixPayloadBuilder.SanitizePixText(product?.User?.FullName ?? product?.User?.UserName ?? "OTSERV MARKET", 25);
            var merchantCity = PixPayloadBuilder.SanitizePixText("SAO PAULO", 15);
            var txId = PixPayloadBuilder.BuildPixTxId(productId);
            address = PixPayloadBuilder.BuildPixPayload(recipientPixKey, amount, merchantName, merchantCity, txId);
            paymentId = $"pix-{Guid.NewGuid():N}";
        }

        var record = BuildPaymentRecord(productId, buyerUserId, sellerUserId, currency, address, paymentId, amount, selectedMethod, null);
        return new GenerateAddressResult(address, paymentId, null, record);
    }

    public async Task<PaymentRecord?> FindPaymentByAddressAsync(string address)
    {
        var normalizedAddress = address.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Payments.FirstOrDefaultAsync(p => p.Address == normalizedAddress);
    }

    public async Task<(bool Confirmed, bool AlreadyPaid, decimal ReceivedAmount)> ConfirmPaymentAsync(PaymentRecord payment)
    {
        return await _confirmationService.ConfirmAsync(payment);
    }

    public async Task SavePaymentRecordAsync(PaymentRecord record)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Payments.Add(record);
        await db.SaveChangesAsync();
    }

    public async Task LogAsync(string message, string? userId = null, Exception? ex = null)
    {
        await _logService.LogAsync(message, source: "Payment", level: ex != null ? "Error" : "Info", userId: userId, ex: ex);
    }

    public async Task LogWarningAsync(string message, string? userId = null)
    {
        await _logService.LogAsync(message, source: "Payment", level: "Warning", userId: userId);
    }

    private static PaymentRecord BuildPaymentRecord(
        int productId,
        string? buyerUserId,
        string? sellerUserId,
        string currency,
        string address,
        string paymentId,
        decimal amount,
        string paymentMethod,
        string? privateKey)
    {
        return new PaymentRecord
        {
            ProductId = productId,
            UserId = buyerUserId,
            SellerId = sellerUserId,
            Currency = currency,
            Address = address,
            PaymentId = paymentId,
            Amount = amount,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow,
            PaymentMethod = paymentMethod,
            PrivateKey = privateKey,
        };
    }

    private async Task<string?> ResolvePixRecipientKeyAsync(bool useSiteIntermediary, string? sellerUserId, Product? product)
    {
        if (useSiteIntermediary)
        {
            var sitePixKey = await _adminSettingsService.GetSiteIntermediaryPixKeyAsync();
            return string.IsNullOrWhiteSpace(sitePixKey) ? null : sitePixKey.Trim();
        }

        if (string.IsNullOrWhiteSpace(sellerUserId))
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var sellerPixKey = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == sellerUserId)
            .Select(u => u.PixKey)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(sellerPixKey) ? null : sellerPixKey.Trim();
    }
}
