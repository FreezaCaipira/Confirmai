using System.Globalization;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

public sealed record EventPaymentLoadResult
{
    public EventConfirmation? Confirmation { get; init; }
    public bool IsAdminViewing { get; init; }
    public bool GroupGatewaysEnabled { get; init; }
    public List<EventPaymentGatewayOption> AvailableGateways { get; init; } = new();
    public string? RedirectUrl { get; init; }
    public string? PendingBrCode { get; init; }
    public string? PendingChargeId { get; init; }
    public string? PendingGatewayName { get; init; }
    public bool ShowDirectPixToOrganizer { get; init; }
}

public sealed record EventPaymentGenerateResult(
    bool Success,
    string? BrCode,
    string? ChargeId,
    string? GatewayName,
    string? ErrorMessage);

public sealed record EventPaymentPollResult(
    bool IsPaid,
    EventConfirmation? UpdatedConfirmation = null);

public sealed class EventPaymentService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EventPaymentGatewayFactory _gatewayFactory;
    private readonly FeeOptions _feeOptions;

    public EventPaymentService(
        IDbContextFactory<AppDbContext> dbFactory,
        EventPaymentGatewayFactory gatewayFactory,
        IOptions<FeeOptions> feeOptions)
    {
        _dbFactory = dbFactory;
        _gatewayFactory = gatewayFactory;
        _feeOptions = feeOptions.Value;
    }

    public async Task<EventPaymentLoadResult> LoadConfirmationAsync(int confirmationId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var groupAdminIds = await db.GroupMembers
            .Where(m => m.UserId == userId && m.Role == GroupMemberRole.Admin)
            .Select(m => m.GroupId)
            .ToListAsync();

        var conf = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group).ThenInclude(g => g.Members).ThenInclude(m => m.User)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == confirmationId &&
                (c.UserId == userId || groupAdminIds.Contains(c.Event.GroupId)));

        var isAdminViewing = conf is not null && conf.UserId != userId;
        var groupGatewaysEnabled = conf?.Event.Group.EnablePaymentGateways ?? false;
        var availableGateways = new List<EventPaymentGatewayOption>();

        if (groupGatewaysEnabled)
        {
            availableGateways = (await _gatewayFactory.GetAvailableAsync()).ToList();
        }

        string? redirectUrl = null;
        if (conf is not null && !isAdminViewing && (conf.Event.Price is null || conf.Position == FutsalPosition.Goalkeeper))
        {
            redirectUrl = conf.Event.Sport == Sport.Futsal
                ? $"/futsal/{conf.Event.Id}"
                : $"/poker/{conf.Event.Id}";
        }

        string? pendingBrCode = null;
        string? pendingChargeId = null;
        string? pendingGatewayName = null;

        if (conf is not null &&
            conf.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
            conf.PixTxId is not null &&
            conf.PixBrCode is not null)
        {
            pendingBrCode = conf.PixBrCode;
            pendingChargeId = conf.PixTxId;
            pendingGatewayName = conf.PaymentGatewayName;
        }

        return new EventPaymentLoadResult
        {
            Confirmation = conf,
            IsAdminViewing = isAdminViewing,
            GroupGatewaysEnabled = groupGatewaysEnabled,
            AvailableGateways = availableGateways,
            RedirectUrl = redirectUrl,
            PendingBrCode = pendingBrCode,
            PendingChargeId = pendingChargeId,
            PendingGatewayName = pendingGatewayName,
            ShowDirectPixToOrganizer = _feeOptions.ShowDirectPixToOrganizer
        };
    }

    public async Task<EventPaymentGenerateResult> GeneratePixChargeAsync(
        int confirmationId,
        string selectedGatewayName,
        bool groupGatewaysEnabled)
    {
        if (!groupGatewaysEnabled)
        {
            return new EventPaymentGenerateResult(
                false, null, null, null,
                "Gateways de pagamento estão desativados para este grupo.");
        }

        if (_feeOptions.IsConfigured &&
            !_feeOptions.SupportedGateways.Contains(selectedGatewayName, StringComparer.OrdinalIgnoreCase))
        {
            return new EventPaymentGenerateResult(
                false, null, null, null,
                "O gateway selecionado não suporta cobrança de taxa. Entre em contato com o administrador.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var conf = await db.EventConfirmations
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null || conf.Event.Price is null)
        {
            return new EventPaymentGenerateResult(false, null, null, null, "Confirmação não encontrada.");
        }

        if (_feeOptions.IsConfigured)
        {
            var existingPayoutAccount = await db.GroupPayoutAccounts
                .FirstOrDefaultAsync(gpa => gpa.GroupId == conf.Event.GroupId && gpa.IsActive);

            if (existingPayoutAccount is null)
            {
                return new EventPaymentGenerateResult(
                    false, null, null, null,
                    "O organizador não configurou a chave PIX para repasse. Entre em contato com o administrador.");
            }
        }

        var gateway = await _gatewayFactory.GetGatewayAsync(selectedGatewayName);
        if (gateway is null)
        {
            return new EventPaymentGenerateResult(
                false, null, null, null,
                "Gateway indisponível ou desativado pelo administrador.");
        }

        var chargeAmount = conf.Event.Price.Value;
        GroupPayoutAccount? payoutAccount = null;

        if (_feeOptions.IsConfigured &&
            _feeOptions.SupportedGateways.Contains(selectedGatewayName, StringComparer.OrdinalIgnoreCase))
        {
            chargeAmount = chargeAmount + _feeOptions.AppFeeFixed + _feeOptions.GatewayFeeFixed;

            payoutAccount = await db.GroupPayoutAccounts
                .FirstOrDefaultAsync(pa => pa.GroupId == conf.Event.GroupId && pa.IsActive);
        }

        var charge = await gateway.CreateChargeAsync(chargeAmount, conf.Id, payoutAccount);

        var entity = await db.EventConfirmations.FindAsync(conf.Id);
        if (entity is not null)
        {
            entity.PixTxId = charge.ChargeId;
            entity.PixBrCode = charge.BrCode;
            entity.PaymentGatewayName = gateway.Name;
            entity.PaymentStatus = EventConfirmationPaymentStatus.Pending;
            entity.HasPaid = false;
            await db.SaveChangesAsync();
        }

        return new EventPaymentGenerateResult(
            true, charge.BrCode, charge.ChargeId, gateway.Name, null);
    }

    public async Task<EventPaymentPollResult> CheckPaymentStatusAsync(
        int confirmationId,
        string chargeId,
        string gatewayName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fresh = await db.EventConfirmations.FindAsync(confirmationId);
        if (fresh?.PaymentStatus == EventConfirmationPaymentStatus.Paid)
        {
            return new EventPaymentPollResult(true, fresh);
        }

        var gateway = await _gatewayFactory.GetGatewayAsync(gatewayName);
        if (gateway is not null && await gateway.IsChargePaidAsync(chargeId))
        {
            var entity = await db.EventConfirmations.FindAsync(confirmationId);
            if (entity is not null)
            {
                entity.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                entity.HasPaid = true;
                await db.SaveChangesAsync();
                return new EventPaymentPollResult(true, entity);
            }
        }

        return new EventPaymentPollResult(false);
    }

    public static string? GetGroupAdminPixKey(Group group)
    {
        if (!string.IsNullOrWhiteSpace(group.PixReceiverUserId))
        {
            var chosen = group.Members
                .FirstOrDefault(m => m.UserId == group.PixReceiverUserId);
            if (!string.IsNullOrWhiteSpace(chosen?.User?.PixKey))
                return chosen.User.PixKey;
        }

        return group.Members
            .Where(m => m.Role == GroupMemberRole.Admin && !string.IsNullOrWhiteSpace(m.User?.PixKey))
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.User!.PixKey)
            .FirstOrDefault();
    }

    public static string BuildPixStaticPayload(string pixKey, string groupName, string? city, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(pixKey))
            return string.Empty;

        static string F(string tag, string v) => $"{tag}{v.Length:D2}{v}";

        var name = groupName.Length > 25 ? groupName[..25] : groupName;
        var cityStr = string.IsNullOrWhiteSpace(city) ? "Brasil" : (city.Length > 15 ? city[..15] : city);
        var amtStr = amount.ToString("F2", CultureInfo.InvariantCulture);

        var mai = F("0014", "br.gov.bcb.pix") + F("01", pixKey);
        var body = "000201"
            + F("26", mai)
            + "52040000"
            + "5303986"
            + F("54", amtStr)
            + "5802BR"
            + F("59", name)
            + F("60", cityStr)
            + "6304";

        ushort crc = 0xFFFF;
        foreach (char c in body)
        {
            crc ^= (ushort)(c << 8);
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return body + crc.ToString("X4");
    }

}
