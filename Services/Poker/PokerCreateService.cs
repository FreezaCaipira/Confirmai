using System.Security.Claims;
using System.Security.Cryptography;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Poker;

public sealed class PokerCreateInitData
{
    public Group? PreselectedGroup { get; set; }
    public bool GroupHasPixKey { get; set; }
    public string? AdminUserId { get; set; }

    /// <summary>C39-A (D3): faixa da taxa % do torneio; o form abre no minimo.</summary>
    public decimal FeePercentMin { get; set; }
    public decimal FeePercentMax { get; set; }
}

public sealed record PokerCreateResult(bool Success, string? Error, string? CollisionHref, int? EventId);

/// <summary>C39-B: mesa/faixa de preco de um cash game vinda do formulario.</summary>
public sealed record CashTableInput(string Label, decimal Price, decimal PlatformFeePercent);

public sealed class PokerCreateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly EventCollisionService _collisionService;
    private readonly UiTextService _ui;
    private readonly PlatformFeePolicy _feePolicy;

    public PokerCreateService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        EventCollisionService collisionService,
        UiTextService ui,
        PlatformFeePolicy feePolicy)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _collisionService = collisionService;
        _ui = ui;
        _feePolicy = feePolicy;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<PokerCreateInitData> InitializeAsync(int? groupId, string eventName, string city, string stateCode)
    {
        if (!groupId.HasValue)
            return new PokerCreateInitData
            {
                FeePercentMin = _feePolicy.PokerFeePercentMin,
                FeePercentMax = _feePolicy.PokerFeePercentMax,
            };

        var userId = await GetCurrentUserIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var group = await db.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId.Value
                && g.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin));

        if (group is null)
            return new PokerCreateInitData
            {
                FeePercentMin = _feePolicy.PokerFeePercentMin,
                FeePercentMax = _feePolicy.PokerFeePercentMax,
            };

        return new PokerCreateInitData
        {
            PreselectedGroup = group,
            GroupHasPixKey = !string.IsNullOrWhiteSpace(
                EventPaymentService.GetGroupAdminPixKey(group)),
            AdminUserId = userId,
            FeePercentMin = _feePolicy.PokerFeePercentMin,
            FeePercentMax = _feePolicy.PokerFeePercentMax,
        };
    }

    /// <summary>Valor anunciado do evento: buy-in do torneio, maior preco de
    /// mesa do cash game (legado sem mesas cai para o stack minimo); home
    /// game nunca cobra.</summary>
    private static decimal ChargedAmount(PokerCreateFormData form) => form.EventType switch
    {
        PokerEventType.Tournament => form.BuyInAmount,
        PokerEventType.CashGame   => Math.Max(
            form.CashTables.Count > 0 ? form.CashTables.Max(t => t.Price) : 0m,
            form.CashMinBuyIn),
        _ => 0,
    };

    public async Task<PokerCreateResult> SaveAsync(PokerCreateFormData form, Group? preselectedGroup)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            return new(false, "Voce precisa estar autenticado.", null, null);

        var startsAt = DateTime.SpecifyKind(form.Date.ToDateTime(form.Time), DateTimeKind.Utc);

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Mesma regra do futsal (C36-E): evento com valor exige Pix resolvivel
        // no nivel do GRUPO (recebedor escolhido -> qualquer admin com Pix).
        // A checagem vem antes de qualquer SaveChanges para nao deixar grupo
        // orfao quando o grupo e criado inline.
        Group? existingGroup = null;
        if (preselectedGroup is not null)
        {
            existingGroup = await db.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == preselectedGroup.Id)
                ?? preselectedGroup;
        }

        // C39-A F3 (D3): a taxa % do torneio e validada no service dentro da
        // faixa do FeeOptions — a tela so sugere o minimo.
        if (form.EventType == PokerEventType.Tournament
            && form.BuyInAmount > 0
            && !_feePolicy.IsPokerFeePercentInRange(form.PlatformFeePercent))
        {
            return new(false,
                _ui.Get("Poker.Create.FeePercentOutOfRange",
                    _feePolicy.PokerFeePercentMin, _feePolicy.PokerFeePercentMax),
                null, null);
        }

        // C39-B F8: mesas do cash — rotulo obrigatorio e % de cada mesa
        // dentro da mesma faixa do torneio. Validado antes de persistir.
        if (form.EventType == PokerEventType.CashGame)
        {
            if (form.CashTables.Any(t => string.IsNullOrWhiteSpace(t.Label)))
                return new(false, _ui["Poker.Create.TableLabelRequired"], null, null);
            if (form.CashTables.Any(t => t.Price < 0m))
                return new(false, _ui["Poker.Create.TablePriceInvalid"], null, null);
            if (form.CashTables.Any(t => t.Price > 0m && !_feePolicy.IsPokerFeePercentInRange(t.PlatformFeePercent)))
            {
                return new(false,
                    _ui.Get("Poker.Create.FeePercentOutOfRange",
                        _feePolicy.PokerFeePercentMin, _feePolicy.PokerFeePercentMax),
                    null, null);
            }
        }

        if (ChargedAmount(form) > 0 && !(existingGroup?.EnablePaymentGateways ?? false))
        {
            var pixKey = existingGroup is not null
                ? EventPaymentService.GetGroupAdminPixKey(existingGroup)
                : (await db.Users.FirstOrDefaultAsync(u => u.Id == userId))?.PixKey;

            if (string.IsNullOrWhiteSpace(pixKey))
            {
                return new(false,
                    _ui["Poker.Create.PixRequired"],
                    "/profile/" + userId + "?intent=pix",
                    null);
            }
        }

        Group group;
        if (existingGroup is not null)
        {
            group = existingGroup;
        }
        else
        {
            group = new Group
            {
                Name = form.Name,
                Sport = Sport.Poker,
                City = form.City,
                StateCode = form.StateCode.ToUpperInvariant(),
                IsActive = true,
                IsPrivate = true,
                CreatedByUserId = userId,
                InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = userId, Role = GroupMemberRole.Admin });
        }

        var collision = await _collisionService.FindGroupTimeCollisionAsync(group.Id, startsAt);
        if (collision is not null)
            return new(false, EventCollisionService.BuildConflictMessage(collision.StartsAt), $"/poker/{collision.EventId}", null);

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Poker,
            Location = form.Address ?? string.Empty,
            PokerHouseName = form.PokerHouseName,
            StartsAt = startsAt,
            MaxPlayers = form.MaxPlayers,
            PokerEventType = form.EventType,
            Modality = form.EventType != PokerEventType.HomeGame ? form.Modality : null,
            IsActive = true,
            CreatedByUserId = userId,
        };

        switch (form.EventType)
        {
            case PokerEventType.Tournament:
                ev.BuyInAmount = form.BuyInAmount;
                // C39-A F3: o torneio cobra pelo app — Price alimenta todo o
                // caminho do dinheiro; cash cobra por mesa (C39-B) e home
                // game nunca cobra. Poker antigo sem Price nao cobra retroativo.
                ev.Price = form.BuyInAmount > 0 ? form.BuyInAmount : null;
                ev.PlatformFeePercent = form.BuyInAmount > 0 ? form.PlatformFeePercent : null;
                ev.GTD = form.GTD;
                ev.RebuyAmount = form.RebuyAmount;
                ev.RebuyDoubleAmount = form.RebuyDoubleAmount;
                ev.AddonAmount = form.AddonAmount;
                ev.AddonDoubleAmount = form.AddonDoubleAmount;
                ev.StartingStack = form.StartingStack;
                ev.InitialBlindBB = form.InitialBlindBB;
                if (form.LateRegDate.HasValue && form.LateRegTime.HasValue)
                    ev.LateRegEndsAt = DateTime.SpecifyKind(
                        form.LateRegDate.Value.ToDateTime(form.LateRegTime.Value), DateTimeKind.Utc);
                break;
            case PokerEventType.CashGame:
                // C39-B F8: o cash cobra por mesa (EventPriceOption); sem mesas
                // o evento segue sem cobranca pelo app. CashMin/CashMax viram
                // informativos derivados das mesas quando elas existem.
                if (form.CashTables.Count > 0)
                {
                    var sort = 0;
                    foreach (var t in form.CashTables)
                    {
                        ev.PriceOptions.Add(new EventPriceOption
                        {
                            Label = t.Label.Trim(),
                            Price = t.Price,
                            PlatformFeePercent = t.PlatformFeePercent,
                            SortOrder = sort++,
                            IsActive = true,
                        });
                    }
                    ev.CashMinBuyIn = form.CashTables.Min(t => t.Price);
                    ev.CashMaxBuyIn = form.CashTables.Max(t => t.Price);
                }
                else
                {
                    ev.CashMinBuyIn = form.CashMinBuyIn;
                    ev.CashMaxBuyIn = form.CashMaxBuyIn;
                }
                ev.CashIncludes = form.CashIncludes;
                break;
            case PokerEventType.HomeGame:
                ev.HomeGameCode = GenerateHomeGameCode();
                break;
        }

        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return new(true, null, null, ev.Id);
    }

    public static string GenerateHomeGameCode()
    {
        const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string Digits = "23456789";
        var bytes = RandomNumberGenerator.GetBytes(6);
        return $"{Letters[bytes[0] % Letters.Length]}{Letters[bytes[1] % Letters.Length]}{Digits[bytes[2] % Digits.Length]}-{Digits[bytes[3] % Digits.Length]}{Digits[bytes[4] % Digits.Length]}{Digits[bytes[5] % Digits.Length]}";
    }
}

public sealed class PokerCreateFormData
{
    public PokerEventType EventType { get; set; } = PokerEventType.Tournament;
    public string Name { get; set; } = string.Empty;
    public string PokerHouseName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public TimeOnly Time { get; set; } = new(19, 0);
    public DateOnly? LateRegDate { get; set; }
    public TimeOnly? LateRegTime { get; set; }
    public PokerModality Modality { get; set; } = PokerModality.Vanilla;
    public int StartingStack { get; set; } = 20000;
    public int InitialBlindBB { get; set; } = 50;
    public int MaxPlayers { get; set; }
    public decimal BuyInAmount { get; set; }
    public decimal PlatformFeePercent { get; set; }
    public decimal? GTD { get; set; }
    public decimal? RebuyAmount { get; set; }
    public decimal? RebuyDoubleAmount { get; set; }
    public decimal? AddonAmount { get; set; }
    public decimal? AddonDoubleAmount { get; set; }
    public decimal CashMinBuyIn { get; set; }
    public decimal CashMaxBuyIn { get; set; }

    /// <summary>C39-B: mesas do cash game (rotulo, preco, % da taxa).</summary>
    public List<CashTableInput> CashTables { get; set; } = new();

    public string? CashIncludes { get; set; }
    public bool IsPrivate { get; set; } = true;
}
