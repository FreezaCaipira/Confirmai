using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Futsal;

public sealed class FutsalCreateInitData
{
    public Group? PreselectedGroup { get; set; }
    public List<Venue> Venues { get; set; } = new();
}

public sealed record FutsalCreateResult(
    bool Success,
    string? Error,
    string? CollisionHref,
    int? EventId);

public sealed class FutsalCreateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly EventCollisionService _collisionService;
    private readonly LogService _logService;

    public FutsalCreateService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        EventCollisionService collisionService,
        LogService logService)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _collisionService = collisionService;
        _logService = logService;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<FutsalCreateInitData> InitializeAsync(int? groupId)
    {
        if (!groupId.HasValue)
            return new FutsalCreateInitData();

        var userId = await GetCurrentUserIdAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId.Value
                && g.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin));

        if (group is null)
            return new FutsalCreateInitData();

        var venues = await db.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.City)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return new FutsalCreateInitData { PreselectedGroup = group, Venues = venues };
    }

    public async Task<FutsalCreateResult> SaveAsync(
        CreateMatchFormData form, List<Venue> venues, Group? preselectedGroup,
        string subFormat)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            return new FutsalCreateResult(false, "Voce precisa estar autenticado.", null, null);

        var venue = venues.FirstOrDefault(v => v.Id == form.VenueId);
        if (venue is null)
            return new FutsalCreateResult(false, "Quadra invalida.", null, null);

        var startsAt = DateTime.SpecifyKind(
            form.Date.ToDateTime(form.Time),
            DateTimeKind.Utc);

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
                Name = form.GroupName,
                Sport = Sport.Futsal,
                City = venue.City,
                StateCode = venue.StateCode,
                IsActive = true,
                IsPrivate = true,
                CreatedByUserId = userId,
                InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            };
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupMemberRole.Admin,
            });
        }

        var collision = await _collisionService.FindGroupTimeCollisionAsync(group.Id, startsAt);
        if (collision is not null)
        {
            return new FutsalCreateResult(
                false,
                EventCollisionService.BuildConflictMessage(collision.StartsAt),
                $"/futsal/{collision.EventId}",
                null);
        }

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            VenueId = venue.Id,
            Location = venue.Address,
            LocalName = form.LocalName,
            StartsAt = startsAt,
            DurationMinutes = form.DurationMinutes,
            Price = form.Price,
            MaxPlayers = form.MaxPlayers,
            MaxGoalkeepers = form.MaxGoalkeepers > 0 ? form.MaxGoalkeepers : null,
            PlayersPerSide = subFormat switch { "society" => 7, "campo" => 11, _ => 5 },
            IsActive = true,
            CreatedByUserId = userId,
        };
        db.Events.Add(ev);

        if (form.RecurrenceEnabled && form.SelectedDays.Any())
        {
            foreach (var dow in form.SelectedDays)
            {
                db.RachaSchedules.Add(new MatchSchedule
                {
                    GroupId = group.Id,
                    VenueId = venue.Id,
                    DayOfWeek = dow,
                    TimeOfDay = form.Time,
                    DurationMinutes = form.DurationMinutes,
                    Price = form.Price,
                    MaxPlayers = form.MaxPlayers,
                    MaxGoalkeepers = form.MaxGoalkeepers > 0 ? form.MaxGoalkeepers : null,
                    LocalName = form.LocalName,
                    IsActive = true,
                    CreatedByUserId = userId,
                });
            }
        }

        await db.SaveChangesAsync();
        await _logService.AuditAsync(
            AuditEvents.EventCreated,
            AuditEntities.Event,
            ev.Id.ToString(),
            $"Partida criada: \"{ev.LocalName ?? ev.Group?.Name}\" em {ev.StartsAt:dd/MM/yyyy HH:mm} (grupo #{ev.GroupId})",
            userId, "EventCreate");

        return new FutsalCreateResult(true, null, null, ev.Id);
    }
}

public sealed class CreateMatchFormData
{
    public string GroupName { get; set; } = string.Empty;
    public string? LocalName { get; set; }
    public int VenueId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public TimeOnly Time { get; set; } = new(20, 0);
    public int DurationMinutes { get; set; } = 90;
    public int MaxPlayers { get; set; } = 10;
    public int MaxGoalkeepers { get; set; } = 2;
    public bool RotateInGoal { get; set; }
    public decimal Price { get; set; }
    public bool RecurrenceEnabled { get; set; }
    public HashSet<DayOfWeek> SelectedDays { get; set; } = new();
    public bool IsPrivate { get; set; } = true;
}
