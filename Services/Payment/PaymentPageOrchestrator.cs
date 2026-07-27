using System.Security.Claims;
using Confirmai.Configuration;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

public sealed record GeneratePaymentResult(
    string Address,
    string PaymentId,
    PaymentRecord PaymentRecord,
    string? Error,
    string? SellerKeyNotice);

public sealed record CheckPaymentResult(
    bool IsPaid,
    string? Message,
    string MessageType);

public sealed class PaymentPageOrchestrator
{
    private readonly PaymentCommandService _commands;
    private readonly AuthenticationStateProvider _authProvider;
    private readonly AdminSettingsService _adminSettings;
    private readonly IOptions<AbacatePayOptions> _abacatePayOpts;

    public PaymentPageOrchestrator(
        PaymentCommandService commands,
        AuthenticationStateProvider authProvider,
        AdminSettingsService adminSettings,
        IOptions<AbacatePayOptions> abacatePayOpts)
    {
        _commands = commands;
        _authProvider = authProvider;
        _adminSettings = adminSettings;
        _abacatePayOpts = abacatePayOpts;
    }

    public async Task<GeneratePaymentResult?> GenerateBtcAddressAsync(
        int productId, decimal amount, string selectedMethod,
        string? sellerUserId, string offerCurrency, decimal minBtcAmount)
    {
        if (amount < minBtcAmount)
        {
            await _commands.LogWarningAsync($"Tentativa de pagamento abaixo do minimo: {amount} BTC.", sellerUserId);
            return new GeneratePaymentResult("", "", null!, $"Valor abaixo do minimo de {minBtcAmount} BTC.", null);
        }

        try
        {
            var userId = await GetActorUserIdAsync();
            var result = await _commands.GenerateBtcAddressAsync(
                productId, amount, selectedMethod, sellerUserId, userId, offerCurrency);

            if (result == null)
                return new GeneratePaymentResult("", "", null!, "Falha ao gerar endereco.", null);

            await _commands.SavePaymentRecordAsync(result.PaymentRecord);
            await _commands.LogAsync($"Endereco/invoice gerado para produto {productId} via {selectedMethod}.", userId);

            return new GeneratePaymentResult(result.Address, result.PaymentId, result.PaymentRecord, null, null);
        }
        catch (Exception ex)
        {
            await _commands.LogAsync("Erro ao gerar endereco/invoice.", sellerUserId, ex);
            return new GeneratePaymentResult("", "", null!, ex.Message, null);
        }
    }

    public async Task<GeneratePaymentResult?> GeneratePixPaymentAsync(
        int productId, decimal amount, string selectedMethod,
        string? sellerUserId, string offerCurrency,
        bool useSiteIntermediary, Product? product)
    {
        try
        {
            var userId = await GetActorUserIdAsync();
            var result = await _commands.GeneratePixPaymentAsync(
                productId, amount, selectedMethod, sellerUserId, userId, offerCurrency,
                _abacatePayOpts.Value.IsEnabled, product, useSiteIntermediary);

            if (result == null)
            {
                string? sellerKeyNotice = null;
                if (!useSiteIntermediary && string.IsNullOrWhiteSpace(await ResolvePixRecipientKeyAsync(sellerUserId, useSiteIntermediary)))
                {
                    sellerKeyNotice = "O vendedor nao possui chave PIX cadastrada no perfil. Escolha intermedio do ADM do site ou use outra forma de pagamento.";
                }
                return new GeneratePaymentResult("", "", null!, sellerKeyNotice == null ? "Nao foi possivel gerar o pagamento PIX." : null, sellerKeyNotice);
            }

            await _commands.SavePaymentRecordAsync(result.PaymentRecord);
            await _commands.LogAsync(
                $"QR PIX gerado para produto {productId} via {(_abacatePayOpts.Value.IsEnabled ? "AbacatePay" : "payload estatico")}.",
                userId);

            return new GeneratePaymentResult(result.Address, result.PaymentId, result.PaymentRecord, null, null);
        }
        catch (Exception ex)
        {
            await _commands.LogAsync("Erro ao gerar pagamento PIX.", sellerUserId, ex);
            return new GeneratePaymentResult("", "", null!, ex.Message, null);
        }
    }

    public async Task<CheckPaymentResult> CheckPaymentAsync(
        string address, bool isPaid, bool isPix, string notFoundMessage,
        string alreadyConfirmedMessage, string confirmedMessage,
        string checkErrorMessage, string pixUnavailableMessage,
        Func<decimal, string> formatAmount)
    {
        if (isPix && !_abacatePayOpts.Value.IsEnabled)
            return new CheckPaymentResult(false, pixUnavailableMessage, "info");

        if (string.IsNullOrWhiteSpace(address))
            return new CheckPaymentResult(false, notFoundMessage, "error");

        if (isPaid)
            return new CheckPaymentResult(true, alreadyConfirmedMessage, "success");

        try
        {
            var payment = await _commands.FindPaymentByAddressAsync(address);
            if (payment == null)
                return new CheckPaymentResult(false, notFoundMessage, "error");

            var result = await _commands.ConfirmPaymentAsync(payment);

            if (result.AlreadyPaid)
                return new CheckPaymentResult(true, alreadyConfirmedMessage, "success");

            if (result.Confirmed)
                return new CheckPaymentResult(true, confirmedMessage, "success");

            return new CheckPaymentResult(false,
                string.Format(formatAmount(result.ReceivedAmount)) + " / " + string.Format(formatAmount(0)),
                "warning");
        }
        catch (Exception ex)
        {
            await _commands.LogAsync("Erro ao verificar pagamento.", null, ex);
            return new CheckPaymentResult(false, checkErrorMessage, "error");
        }
    }

    private async Task<string?> GetActorUserIdAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task<string?> ResolvePixRecipientKeyAsync(string? sellerUserId, bool useSiteIntermediary)
    {
        if (useSiteIntermediary)
        {
            var sitePixKey = await _adminSettings.GetSiteIntermediaryPixKeyAsync();
            return string.IsNullOrWhiteSpace(sitePixKey) ? null : sitePixKey.Trim();
        }

        return null;
    }
}
