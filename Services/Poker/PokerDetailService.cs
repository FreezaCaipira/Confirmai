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
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Sport == Sport.Poker);

        if (ev is null) return new PokerConfirmResult(PokerConfirmStatus.EventNotFound, false);
        if (!ev.IsActive) return new PokerConfirmResult(PokerConfirmStatus.EventCancelled, false);
        if (ev.Confirmations.Any(c => c.UserId == userId))
            return new PokerConfirmResult(PokerConfirmStatus.AlreadyRegistered, false);
        if (ev.Group.IsPrivate && !ev.Group.Members.Any(m => m.UserId == userId))
            return new PokerConfirmResult(PokerConfirmStatus.NotGroupMember, false);

        // Cash game (C39-B) resolve o preco pela mesa escolhida; torneio pelo
        // Event.Price. Home game nao cobra.
        var price = ev.Price;
        var feePercent = ev.PlatformFeePercent;
        if (priceOptionId is int optionId)
        {
            var option = await db.EventPriceOptions
                .FirstOrDefaultAsync(o => o.Id == optionId && o.EventId == eventId && o.IsActive);
            if (option is not null)
            {
                price = option.Price;
                feePercent = option.PlatformFeePercent;
            }
        }

        var now = DateTime.UtcNow;
        var conf = new EventConfirmation
        {
            EventId = eventId,
            UserId = userId,
            ConfirmedAt = now,
            ChargedPrice = price,
            PriceOptionId = priceOptionId,
            PlatformFeeAmount = _feePolicy.ResolveStampForNewConfirmation(ev.Group, price, now, feePercent),
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
}
