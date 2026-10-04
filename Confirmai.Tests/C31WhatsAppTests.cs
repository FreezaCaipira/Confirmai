using System.Net;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Groups;
using Confirmai.Services.Notification;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C31 Fase 1 — sender abstraction (Evolution contract + dry-run), JID
/// validation, and the group-admin-gated JID save.
/// </summary>
public class C31WhatsAppTests
{
    private static (IDbContextFactory<AppDbContext> factory, GroupFeaturesService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"wa-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var gatewayFactory = new EventPaymentGatewayFactory(
            new List<Services.Interfaces.IEventPaymentGateway>(),
            new GatewayService(factory));
        return (factory, new GroupFeaturesService(factory, logService, gatewayFactory));
    }

    // ── WhatsAppGroupJid validator ───────────────────────────────────────────

    [Theory]
    [InlineData("120363012345678901@g.us")]
    [InlineData("5511999999999-1400000000@g.us")]
    public void JidValidator_AcceptsGroupJids(string jid) =>
        Assert.True(WhatsAppGroupJid.IsValid(jid));

    [Theory]
    [InlineData("5511999999999@s.whatsapp.net")] // individual JID, not a group
    [InlineData("120363012345678901")]           // missing @g.us
    [InlineData("abc@g.us")]                     // non-numeric
    [InlineData("123@g.us.evil.com")]            // suffix spoof
    [InlineData("12 34@g.us")]                   // space
    public void JidValidator_RejectsNonGroupJids(string jid) =>
        Assert.False(WhatsAppGroupJid.IsValid(jid));

    // ── EvolutionWhatsAppSender contract ─────────────────────────────────────

    [Fact]
    public async Task EvolutionSender_PostsSendTextWithGroupJidAndApiKeyHeader()
    {
        HttpRequestMessage? captured = null;
        string capturedBody = string.Empty;
        var client = new HttpClient(new RoutingHandler(req =>
        {
            captured = req;
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return HttpTestResponses.Json("{}");
        }));
        var options = Options.Create(new WhatsAppOptions
        {
            Enabled = true,
            DryRun = false,
            BaseUrl = "http://evolution:8080/",
            Instance = "Confirmai-Bot",
            ApiKey = "secret-key"
        });
        var sender = new EvolutionWhatsAppSender(client, options,
            NullLogger<EvolutionWhatsAppSender>.Instance);

        var result = await sender.SendGroupTextAsync("1203630@g.us", "oi grupo");

        Assert.Equal(WhatsAppSendStatus.Sent, result.Status);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("http://evolution:8080/message/sendText/Confirmai-Bot",
            captured.RequestUri!.ToString());
        Assert.True(captured.Headers.TryGetValues("apikey", out var key));
        Assert.Equal("secret-key", Assert.Single(key));

        Assert.Contains("\"number\":\"1203630@g.us\"", capturedBody);
        Assert.Contains("\"text\":\"oi grupo\"", capturedBody);
    }

    [Fact]
    public async Task EvolutionSender_HttpError_ReturnsFailed()
    {
        var client = new HttpClient(new RoutingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var options = Options.Create(new WhatsAppOptions
        {
            Enabled = true, DryRun = false,
            BaseUrl = "http://evolution:8080", Instance = "i", ApiKey = "k"
        });
        var sender = new EvolutionWhatsAppSender(client, options,
            NullLogger<EvolutionWhatsAppSender>.Instance);

        var result = await sender.SendGroupTextAsync("1@g.us", "t");

        Assert.Equal(WhatsAppSendStatus.Failed, result.Status);
    }

    [Fact]
    public async Task EvolutionSender_MissingConfig_ReturnsFailed_WithoutHttp()
    {
        var client = new HttpClient(new RoutingHandler(_ =>
            throw new InvalidOperationException("must not call HTTP")));
        var options = Options.Create(new WhatsAppOptions { Enabled = true, DryRun = false });
        var sender = new EvolutionWhatsAppSender(client, options,
            NullLogger<EvolutionWhatsAppSender>.Instance);

        var result = await sender.SendGroupTextAsync("1@g.us", "t");

        Assert.Equal(WhatsAppSendStatus.Failed, result.Status);
    }

    [Fact]
    public async Task DryRunSender_NeverCallsHttp_AndReportsDryRun()
    {
        var sender = new DryRunWhatsAppSender(NullLogger<DryRunWhatsAppSender>.Instance);

        var result = await sender.SendGroupTextAsync("1203630@g.us", "texto");

        Assert.Equal(WhatsAppSendStatus.DryRun, result.Status);
    }

    // ── SaveWhatsAppGroupJidAsync (service-layer admin check) ────────────────

    private static async Task<int> SeedGroupWithMemberAsync(
        IDbContextFactory<AppDbContext> factory, string userId, GroupMemberRole role)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "G", CreatedByUserId = "creator-1" };
        db.Groups.Add(group);
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = userId });
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = userId, Role = role });
        await db.SaveChangesAsync();
        return group.Id;
    }

    [Fact]
    public async Task SaveJid_GroupAdmin_Saves()
    {
        var (factory, svc) = Setup();
        var groupId = await SeedGroupWithMemberAsync(factory, "admin-1", GroupMemberRole.Admin);

        var (success, _) = await svc.SaveWhatsAppGroupJidAsync(groupId, "1203630@g.us", "admin-1");

        Assert.True(success);
        await using var verify = factory.CreateDbContext();
        Assert.Equal("1203630@g.us", (await verify.Groups.FindAsync(groupId))!.WhatsAppGroupJid);
    }

    [Fact]
    public async Task SaveJid_RegularMember_Rejected()
    {
        var (factory, svc) = Setup();
        var groupId = await SeedGroupWithMemberAsync(factory, "member-1", GroupMemberRole.Member);

        var (success, _) = await svc.SaveWhatsAppGroupJidAsync(groupId, "1203630@g.us", "member-1");

        Assert.False(success);
        await using var verify = factory.CreateDbContext();
        Assert.Null((await verify.Groups.FindAsync(groupId))!.WhatsAppGroupJid);
    }

    [Fact]
    public async Task SaveJid_InvalidFormat_Rejected()
    {
        var (factory, svc) = Setup();
        var groupId = await SeedGroupWithMemberAsync(factory, "admin-1", GroupMemberRole.Admin);

        var (success, _) = await svc.SaveWhatsAppGroupJidAsync(groupId, "not-a-jid", "admin-1");

        Assert.False(success);
        await using var verify = factory.CreateDbContext();
        Assert.Null((await verify.Groups.FindAsync(groupId))!.WhatsAppGroupJid);
    }

    [Fact]
    public async Task SaveJid_Empty_ClearsAndDisables()
    {
        var (factory, svc) = Setup();
        var groupId = await SeedGroupWithMemberAsync(factory, "admin-1", GroupMemberRole.Admin);
        await svc.SaveWhatsAppGroupJidAsync(groupId, "1203630@g.us", "admin-1");

        var (success, _) = await svc.SaveWhatsAppGroupJidAsync(groupId, "", "admin-1");

        Assert.True(success);
        await using var verify = factory.CreateDbContext();
        Assert.Null((await verify.Groups.FindAsync(groupId))!.WhatsAppGroupJid);
    }
}
