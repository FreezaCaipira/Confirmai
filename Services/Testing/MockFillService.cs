using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Confirmai.Services.Testing;

public enum MockFillStatus
{
    Filled,
    Cleared,
    Disabled,
    Forbidden,
    EventNotFound,
    EventFinished,
    NothingToFill,
    NothingToClear,
}

/// <summary>
/// C43-B: preenche uma partida com jogadores de teste do pool mock01..40.
/// Regras (nao simplificar — decisao do Senior):
///  - so com Testing:MockFillEnabled=true E role "admin" (revalidado aqui,
///    nao so na tela);
///  - partida cancelada ou ja iniciada: recusa;
///  - confirma ate lotar linha e goleiros (mesma conta do endpoint dev);
///    idempotente;
///  - ChargedPrice=0, PlatformFeeAmount=0, PlayerFeeAmount=0,
///    PaymentStatus=Paid — sem divida, sem ledger;
///  - remover apaga so as confirmacoes/votos/escalacao dos mocks daquela
///    partida; o pool de usuarios fica.
/// </summary>
public sealed class MockFillService
{
    public const int PoolSize = 40;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TestingOptions _options;
    private readonly LogService _log;

    public MockFillService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<TestingOptions> options,
        LogService log)
    {
        _dbFactory = dbFactory;
        _options = options.Value;
        _log = log;
    }

    public bool Enabled => _options.MockFillEnabled;

    public static string MockEmail(int i) => $"mock{i:D2}@confirmai.invalid";

    public async Task<(MockFillStatus Status, int Count)> FillAsync(int eventId, ClaimsPrincipal user)
    {
        var adminUserId = Authorize(user);
        if (adminUserId is null) return (MockFillStatus.Forbidden, 0);
        if (!_options.MockFillEnabled) return (MockFillStatus.Disabled, 0);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return (MockFillStatus.EventNotFound, 0);
        if (!ev.IsActive || ev.StartsAt <= DateTime.UtcNow)
            return (MockFillStatus.EventFinished, 0);

        var existing = await db.EventConfirmations
            .Where(c => c.EventId == ev.Id)
            .ToListAsync();

        var existingUserIds = existing.Select(c => c.UserId).ToHashSet();
        int gkCount = existing.Count(c => c.Position == FutsalPosition.Goalkeeper);
        int outCount = existing.Count(c => c.Position != FutsalPosition.Goalkeeper);
        int maxGk = ev.MaxGoalkeepers ?? 0;
        int maxOut = ev.MaxPlayers - maxGk;

        // Membros mock do pool que ainda nao sao do grupo entram (escalacao/ranking exigem).
        var members = await db.GroupMembers
            .Where(m => m.GroupId == ev.GroupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();

        int added = 0;
        for (int i = 1; i <= PoolSize && (outCount < maxOut || gkCount < maxGk); i++)
        {
            var mock = await EnsureMockUserAsync(db, i);
            if (existingUserIds.Contains(mock.Id)) continue;

            if (!members.Contains(mock.Id))
            {
                db.GroupMembers.Add(new GroupMember
                {
                    GroupId = ev.GroupId,
                    UserId = mock.Id,
                    Role = GroupMemberRole.Member,
                    CreatedAt = DateTime.UtcNow,
                });
                members.Add(mock.Id);
            }

            bool asGk = gkCount < maxGk;
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = ev.Id,
                UserId = mock.Id,
                Position = asGk ? FutsalPosition.Goalkeeper : FutsalPosition.Outfield,
                ConfirmedAt = DateTime.UtcNow,
                ChargedPrice = 0m,
                PlatformFeeAmount = 0m,
                PlayerFeeAmount = 0m,
                PaymentStatus = EventConfirmationPaymentStatus.Paid,
                HasPaid = true,
            });
            if (asGk) gkCount++; else outCount++;
            existingUserIds.Add(mock.Id);
            added++;
        }

        if (added == 0)
        {
            await db.SaveChangesAsync();
            return (MockFillStatus.NothingToFill, 0);
        }

        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.EventMockFilled,
            AuditEntities.Event,
            ev.Id.ToString(),
            $"Partida #{ev.Id} completada com {added} jogador(es) de teste.",
            actorUserId: adminUserId,
            source: "MockFill",
            metadata: new { eventId = ev.Id, added });

        return (MockFillStatus.Filled, added);
    }

    public async Task<(MockFillStatus Status, int Count)> ClearAsync(int eventId, ClaimsPrincipal user)
    {
        var adminUserId = Authorize(user);
        if (adminUserId is null) return (MockFillStatus.Forbidden, 0);
        if (!_options.MockFillEnabled) return (MockFillStatus.Disabled, 0);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return (MockFillStatus.EventNotFound, 0);

        var mockConfs = await db.EventConfirmations
            .Where(c => c.EventId == ev.Id && c.User!.IsMock)
            .Select(c => new { c.Id, c.UserId })
            .ToListAsync();

        if (mockConfs.Count == 0) return (MockFillStatus.NothingToClear, 0);

        var mockIds = mockConfs.Select(c => c.UserId).ToList();
        var confIds = mockConfs.Select(c => c.Id).ToList();

        // Votos (em ou de mocks); a escalacao mora no TeamId da propria
        // confirmacao e some junto com ela. RemoveRange (nao ExecuteDelete)
        // para funcionar no InMemory dos testes.
        var votes = await db.PostMatchVotes
            .Where(v => v.EventId == ev.Id && (mockIds.Contains(v.VoterUserId) || mockIds.Contains(v.VotedForUserId)))
            .ToListAsync();
        var confsToDelete = await db.EventConfirmations
            .Where(c => confIds.Contains(c.Id))
            .ToListAsync();
        // Mock sem confirmacao em outra partida do grupo sai do grupo: senao
        // fica em membros, contagens e convite como "Teste NN".
        var stillUsedInGroup = await db.EventConfirmations
            .Where(c => c.Event.GroupId == ev.GroupId && c.EventId != ev.Id && mockIds.Contains(c.UserId))
            .Select(c => c.UserId)
            .Distinct()
            .ToListAsync();
        var memberships = await db.GroupMembers
            .Where(m => m.GroupId == ev.GroupId && mockIds.Contains(m.UserId) && !stillUsedInGroup.Contains(m.UserId))
            .ToListAsync();
        db.GroupMembers.RemoveRange(memberships);
        db.PostMatchVotes.RemoveRange(votes);
        db.EventConfirmations.RemoveRange(confsToDelete);

        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.EventMockCleared,
            AuditEntities.Event,
            ev.Id.ToString(),
            $"Jogadores de teste removidos da partida #{ev.Id} ({mockConfs.Count}).",
            actorUserId: adminUserId,
            source: "MockFill",
            metadata: new { eventId = ev.Id, removed = mockConfs.Count });

        return (MockFillStatus.Cleared, mockConfs.Count);
    }

    private static string? Authorize(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return user.IsInRole("admin") ? userId : null;
    }

    private static async Task<ApplicationUser> EnsureMockUserAsync(AppDbContext db, int i)
    {
        var email = MockEmail(i);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is not null) return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = $"Teste {i:D2}",
            IsMock = true,
            // senha aleatoria descartada — ninguem loga com essa conta
            SecurityStamp = Guid.NewGuid().ToString("N"),
            PasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
