using System.Security.Claims;
using System.Security.Cryptography;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Poker;

public sealed class PokerCreateInitData
{
    public Group? PreselectedGroup { get; set; }
}

public sealed record PokerCreateResult(bool Success, string? Error, string? CollisionHref, int? EventId);

public sealed class PokerCreateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly EventCollisionService _collisionService;

    public PokerCreateService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        EventCollisionService collisionService)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _collisionService = collisionService;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<PokerCreateInitData> InitializeAsync(int? groupId, string eventName, string city, string stateCode)
    {
        if (!groupId.HasValue)
            return new PokerCreateInitData();

        var userId = await GetCurrentUserIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId.Value
                && g.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin));

        return new PokerCreateInitData { PreselectedGroup = group };
    }

    public async Task<PokerCreateResult> SaveAsync(PokerCreateFormData form, Group? preselectedGroup)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            return new(false, "Voce precisa estar autenticado.", null, null);

        var startsAt = DateTime.SpecifyKind(form.Date.ToDateTime(form.Time), DateTimeKind.Utc);

        await using var db = await _dbFactory.CreateDbContextAsync();

        Group group;
        if (preselectedGroup is not null)
        {
            group = preselectedGroup;
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
                ev.CashMinBuyIn = form.CashMinBuyIn;
                ev.CashMaxBuyIn = form.CashMaxBuyIn;
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
    public decimal? GTD { get; set; }
    public decimal? RebuyAmount { get; set; }
    public decimal? RebuyDoubleAmount { get; set; }
    public decimal? AddonAmount { get; set; }
    public decimal? AddonDoubleAmount { get; set; }
    public decimal CashMinBuyIn { get; set; }
    public decimal CashMaxBuyIn { get; set; }
    public string? CashIncludes { get; set; }
    public bool IsPrivate { get; set; } = true;
}
