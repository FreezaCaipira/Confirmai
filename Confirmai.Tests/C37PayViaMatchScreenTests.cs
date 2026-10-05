using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C37 F6 — "Pagar" leva para a tela da partida (contexto: elenco, valor,
/// escalação), e a tela da partida é a única entrada para o checkout.
/// O poker não tinha CTA de pagamento — entradas de "Meus pagamentos" de um
/// grupo de poker chegavam a /pagamento/evento/{id} sem contexto nenhum.
/// </summary>
public class C37PayViaMatchScreenTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C37PayViaMatchScreenTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    private async Task<(int eventId, int confId)> SeedPokerEventAsync(
        string adminId, string playerId, decimal price = 30m)
    {
        await _factory.EnsureUserAsync(adminId);
        await _factory.EnsureUserAsync(playerId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = new Group
        {
            Name = "Poker do C37",
            Sport = Sport.Poker,
            CreatedByUserId = adminId,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        db.GroupMembers.AddRange(
            new GroupMember { GroupId = group.Id, UserId = adminId, Role = GroupMemberRole.Admin },
            new GroupMember { GroupId = group.Id, UserId = playerId, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Poker,
            PokerEventType = PokerEventType.CashGame,
            Location = "Clube",
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 9,
            Price = price,
            IsActive = true,
            CreatedByUserId = adminId,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var conf = new EventConfirmation
        {
            EventId = ev.Id,
            UserId = playerId,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            ConfirmedAt = DateTime.UtcNow,
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        return (ev.Id, conf.Id);
    }

    private static HttpClient ClientFor(IntegrationTestWebAppFactory factory, string userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "player");
        return client;
    }

    [Fact]
    public async Task PokerDetail_UnpaidConfirmation_ShowsPayCta()
    {
        var (eventId, confId) = await SeedPokerEventAsync("c37-admin-1", "c37-player-1");

        var client = ClientFor(_factory, "c37-player-1");
        var html = await (await client.GetAsync($"/poker/{eventId}"))
            .Content.ReadAsStringAsync();

        Assert.Contains($"/pagamento/evento/{confId}", html);
        Assert.Contains("pay-btn", html);
    }

    [Fact]
    public async Task PokerDetail_PaidConfirmation_ShowsPaidLabel_NotPayCta()
    {
        var (eventId, _) = await SeedPokerEventAsync("c37-admin-2", "c37-player-2");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
            conf.HasPaid = true;
            conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
            await db.SaveChangesAsync();
        }

        var client = ClientFor(_factory, "c37-player-2");
        var html = await (await client.GetAsync($"/poker/{eventId}"))
            .Content.ReadAsStringAsync();

        Assert.DoesNotContain("/pagamento/evento/", html);
    }

    [Fact]
    public async Task MyPayments_PayButton_LinksToMatchScreen_NotCheckout()
    {
        var (eventId, _) = await SeedPokerEventAsync("c37-admin-3", "c37-player-3");

        var client = ClientFor(_factory, "c37-player-3");

        int groupId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            groupId = await db.Events.Where(e => e.Id == eventId).Select(e => e.GroupId).SingleAsync();
        }

        var html = await (await client.GetAsync($"/grupo/{groupId}/pagamentos"))
            .Content.ReadAsStringAsync();

        // O botão Pagar abre a partida — nunca o checkout direto.
        Assert.Contains($"href=\"/poker/{eventId}\" class=\"btn-pay\"", html);
        Assert.DoesNotContain("btn-pay\" href=\"/pagamento/", html);
    }
}
