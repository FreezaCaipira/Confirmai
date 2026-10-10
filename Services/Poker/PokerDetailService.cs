using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Poker;

public enum PokerConfirmStatus
{
    Confirmed,
    EventNotFound,
    EventCancelled,
    AlreadyRegistered,
    NotGroupMember,
    PriceOptionRequired,
    InvalidPriceOption,
}

public enum PokerCancelStatus
{
    Cancelled,
    NotRegistered,
    PaidOrProofSent,
}

public sealed record PokerConfirmResult(PokerConfirmStatus Status, bool IsWaitlisted)
{
    public bool Success => Status == PokerConfirmStatus.Confirmed;
}

public sealed record PokerCancelResult(PokerCancelStatus Status)
{
    public bool Success => Status == PokerCancelStatus.Cancelled;
}

public enum PokerChangeTableStatus
{
    Changed,
    NotRegistered,
    PaidOrProofSent,
    InvalidPriceOption,
    EventNotFound,
}

public sealed record PokerChangeTableResult(PokerChangeTableStatus Status)
{
    public bool Success => Status == PokerChangeTableStatus.Changed;
}

/// <summary>
/// C39-A F1: inscricao do poker extraida da page para o service (mesmo padrao
/// do <see cref="Futsal.EventDetailService"/>). Carimba o preco cobrado e a
/// taxa da plataforma na criacao da confirmacao — editar o evento depois nao
/// muda o que o jogador ja deve.
/// </summary>
public sealed class PokerDetailService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly PlatformFeePolicy _feePolicy;

    public PokerDetailService(IDbContextFactory<AppDbContext> dbFactory, PlatformFeePolicy feePolicy)
    {
        _dbFactory = dbFactory;
        _feePolicy = feePolicy;
    }

    public async Task<PokerConfirmResult> ConfirmAsync(int eventId, string userId, int? priceOptionId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .Include(e => e.Confirmations)
            .Include(e => e.PriceOptions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Sport == Sport.Poker);

        if (ev is null) return new PokerConfirmResult(PokerConfirmStatus.EventNotFound, false);
        if (!ev.IsActive) return new PokerConfirmResult(PokerConfirmStatus.EventCancelled, false);
        if (ev.Confirmations.Any(c => c.UserId == userId))
            return new PokerConfirmResult(PokerConfirmStatus.AlreadyRegistered, false);
        if (ev.Group.IsPrivate && !ev.Group.Members.Any(m => m.UserId == userId))
            return new PokerConfirmResult(PokerConfirmStatus.NotGroupMember, false);

        // C39-B F9: no cash o preco vem da mesa escolhida. Opcao invalida,
        // inativa ou de outro evento recusa a inscricao (delta da review
        // C39-A — antes gravava o id e caia no preco do evento). Cash com
        // mesas ativas exige escolha; sem mesas segue sem cobranca.
        var price = ev.Price;
        var feePercent = ev.PlatformFeePercent;
        var activeOptions = ev.PriceOptions.Where(o => o.IsActive).ToList();
        if (priceOptionId is int optionId)
        {
            var option = activeOptions.FirstOrDefault(o => o.Id == optionId);
            if (option is null)
                return new PokerConfirmResult(PokerConfirmStatus.InvalidPriceOption, false);
            price = option.Price;
            feePercent = option.PlatformFeePercent;
        }
        else if (ev.PokerEventType == PokerEventType.CashGame && activeOptions.Count > 0)
        {
            return new PokerConfirmResult(PokerConfirmStatus.PriceOptionRequired, false);
        }

        var now = DateTime.UtcNow;
        var stamps = _feePolicy.ResolveStampsForNewConfirmation(ev.Group, price, now, feePercent);
        var conf = new EventConfirmation
        {
            EventId = eventId,
            UserId = userId,
            ConfirmedAt = now,
            ChargedPrice = price,
            PriceOptionId = priceOptionId,
            PlatformFeeAmount = stamps.PlatformFee,
            PlayerFeeAmount = stamps.PlayerFee,
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        ev.Confirmations.Add(conf);
        return new PokerConfirmResult(PokerConfirmStatus.Confirmed, EventCharge.IsWaitlisted(ev, conf));
    }

    /// <summary>
    /// Cancelamento pelo proprio jogador. C39-A F6: confirmacao paga ou com
    /// comprovante enviado nao pode ser apagada pelo jogador — o pagamento
    /// sumiria do controle do organizador. A remocao por admin segue fora
    /// deste metodo (e auditada).
    /// </summary>
    public async Task<PokerCancelResult> CancelAsync(int eventId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var conf = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == userId);
        if (conf is null) return new PokerCancelResult(PokerCancelStatus.NotRegistered);
        if (conf.HasPaid || conf.PixProofUploadedAt is not null)
            return new PokerCancelResult(PokerCancelStatus.PaidOrProofSent);

        db.EventConfirmations.Remove(conf);
        await db.SaveChangesAsync();
        return new PokerCancelResult(PokerCancelStatus.Cancelled);
    }

    /// <summary>
    /// C39-B F9: troca de mesa no cash — recarimba preco e taxa de quem ainda
    /// nao pagou nem enviou comprovante; depois disso a mesa fica travada.
    /// </summary>
    public async Task<PokerChangeTableResult> ChangeTableAsync(int eventId, string userId, int priceOptionId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .Include(e => e.PriceOptions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Sport == Sport.Poker);
        if (ev is null || ev.PokerEventType != PokerEventType.CashGame)
            return new PokerChangeTableResult(PokerChangeTableStatus.EventNotFound);

        var conf = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == userId);
        if (conf is null) return new PokerChangeTableResult(PokerChangeTableStatus.NotRegistered);
        if (conf.HasPaid || conf.PixProofUploadedAt is not null)
            return new PokerChangeTableResult(PokerChangeTableStatus.PaidOrProofSent);

        var option = ev.PriceOptions.FirstOrDefault(o => o.Id == priceOptionId && o.IsActive);
        if (option is null) return new PokerChangeTableResult(PokerChangeTableStatus.InvalidPriceOption);

        conf.PriceOptionId = option.Id;
        conf.ChargedPrice = option.Price;
        var stamps = _feePolicy.ResolveStampsForNewConfirmation(
            ev.Group, option.Price, conf.ConfirmedAt, option.PlatformFeePercent);
        conf.PlatformFeeAmount = stamps.PlatformFee;
        conf.PlayerFeeAmount = stamps.PlayerFee;
        await db.SaveChangesAsync();
        return new PokerChangeTableResult(PokerChangeTableStatus.Changed);
    }
}
