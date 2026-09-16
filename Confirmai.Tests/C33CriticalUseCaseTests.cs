using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// C33 Fase 3 — integration coverage for the critical FALTA use cases from
/// docs/uml/casos-de-uso.md (money, permissions, other users' data), running
/// against real Postgres so FK/constraint bugs surface.
/// </summary>
public class C33CriticalUseCaseTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C33CriticalUseCaseTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Seed helpers ─────────────────────────────────────────────────────

    private async Task<int> SeedEventInGroupAsync(int groupId, string creatorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            GroupId = groupId,
            Sport = Sport.Futsal,
            Location = "Quadra C33",
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 12,
            MaxGoalkeepers = 2,
            IsActive = true,
            CreatedByUserId = creatorId,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    private async Task<int> SeedConfirmationAsync(
        int eventId,
        string userId,
        bool withProof = false,
        decimal? feeAmount = null)
    {
        await _factory.EnsureUserAsync(userId);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conf = new EventConfirmation
        {
            EventId = eventId,
            UserId = userId,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PlatformFeeAmount = feeAmount,
        };
        if (withProof)
        {
            conf.PixProofImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            conf.PixProofContentType = "image/png";
            conf.PixProofUploadedAt = DateTime.UtcNow;
        }
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        return conf.Id;
    }

    private async Task<int> SeedSettlementWithProofAsync(int groupId, string submitterId)
    {
        await _factory.EnsureUserAsync(submitterId);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settlement = new PlatformFeeSettlement
        {
            GroupId = groupId,
            Amount = 15m,
            SubmittedByUserId = submitterId,
            ProofImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            ProofContentType = "image/png",
            Status = PlatformFeeSettlementStatus.EmAnalise,
        };
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();
        return settlement.Id;
    }

    private async Task EnsureRoleAsync(string userId, string roleName)
    {
        await _factory.EnsureUserAsync(userId);
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var result = await userManager.AddToRoleAsync(user, roleName);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static HttpRequestMessage AuthedGet(string url, string? userId = null, string? roles = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (userId is not null) req.Headers.Add("X-Test-UserId", userId);
        if (roles is not null) req.Headers.Add("X-Test-Roles", roles);
        return req;
    }

    // ── UC-O-16: rejeitar comprovante do jogador ─────────────────────────

    [Fact]
    public async Task RejectProofAsync_ClearsProof_KeepsPending_AndWritesAudit()
    {
        var adminId = $"c33-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-payer-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var confId = await SeedConfirmationAsync(eventId, payerId, withProof: true);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<GroupPaymentsService>();
        await service.RejectProofAsync(confId, adminId, groupId);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conf = await db.EventConfirmations.FindAsync(confId);
        Assert.NotNull(conf);
        Assert.Null(conf.PixProofImageData);
        Assert.Null(conf.PixProofContentType);
        Assert.Null(conf.PixProofUploadedAt);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, conf.PaymentStatus);

        var audit = await db.Logs.FirstOrDefaultAsync(l =>
            l.EventType == AuditEvents.EventConfirmationProofRejected &&
            l.EntityId == confId.ToString());
        Assert.NotNull(audit);
        Assert.Equal(adminId, audit.UserId);
    }

    // ── /api/pix-proof/{id}: autorizacao do comprovante do jogador ───────

    [Fact]
    public async Task PixProofEndpoint_Anonymous_Returns401()
    {
        var adminId = $"c33-pp-admin-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var confId = await SeedConfirmationAsync(
            eventId, $"c33-pp-payer-{Guid.NewGuid():N}", withProof: true);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.SendAsync(AuthedGet($"/api/pix-proof/{confId}"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PixProofEndpoint_UnrelatedUser_Returns403()
    {
        var adminId = $"c33-pp-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-pp-payer-{Guid.NewGuid():N}";
        var strangerId = $"c33-pp-stranger-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var confId = await SeedConfirmationAsync(eventId, payerId, withProof: true);
        await _factory.EnsureUserAsync(strangerId);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/pix-proof/{confId}", strangerId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PixProofEndpoint_Payer_ReturnsImage()
    {
        var adminId = $"c33-pp-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-pp-payer-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var confId = await SeedConfirmationAsync(eventId, payerId, withProof: true);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/pix-proof/{confId}", payerId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, body);
    }

    [Fact]
    public async Task PixProofEndpoint_GroupAdmin_ReturnsImage()
    {
        var adminId = $"c33-pp-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-pp-payer-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var confId = await SeedConfirmationAsync(eventId, payerId, withProof: true);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/pix-proof/{confId}", adminId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /api/fee-settlement-proof/{id}: autorizacao do comprovante de repasse ──

    [Fact]
    public async Task SettlementProofEndpoint_Anonymous_Returns401()
    {
        var adminId = $"c33-sp-admin-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var settlementId = await SeedSettlementWithProofAsync(groupId, adminId);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.SendAsync(AuthedGet($"/api/fee-settlement-proof/{settlementId}"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SettlementProofEndpoint_UnrelatedUser_Returns403()
    {
        var adminId = $"c33-sp-admin-{Guid.NewGuid():N}";
        var strangerId = $"c33-sp-stranger-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var settlementId = await SeedSettlementWithProofAsync(groupId, adminId);
        await _factory.EnsureUserAsync(strangerId);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/fee-settlement-proof/{settlementId}", strangerId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SettlementProofEndpoint_Submitter_ReturnsImage()
    {
        var adminId = $"c33-sp-admin-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var settlementId = await SeedSettlementWithProofAsync(groupId, adminId);

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/fee-settlement-proof/{settlementId}", adminId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SettlementProofEndpoint_SystemAdmin_ReturnsImage()
    {
        var adminId = $"c33-sp-admin-{Guid.NewGuid():N}";
        var sysAdminId = $"c33-sysadmin-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var settlementId = await SeedSettlementWithProofAsync(groupId, adminId);
        await EnsureRoleAsync(sysAdminId, "admin");

        var client = _factory.CreateClient();
        var response = await client.SendAsync(AuthedGet($"/api/fee-settlement-proof/{settlementId}", sysAdminId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── UC-O-20 / UC-A-22 / UC-A-23: repasse em Postgres ─────────────────

    [Fact]
    public async Task SubmitSettlementAsync_PersistsEmAnalise_WithItems()
    {
        var adminId = $"c33-st-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-st-payer-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        await SeedConfirmationAsync(eventId, payerId, feeAmount: 15m);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PlatformFeeSettlementService>();
        var result = await service.SubmitSettlementAsync(
            groupId, adminId, 15m,
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png",
            new[] { eventId });

        Assert.True(result.Success, result.Message);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settlement = await db.PlatformFeeSettlements
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.GroupId == groupId);
        Assert.NotNull(settlement);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, settlement.Status);
        Assert.Equal(adminId, settlement.SubmittedByUserId);
        Assert.Single(settlement.Items);
        Assert.Equal(eventId, settlement.Items[0].EventId);
        Assert.Equal(15m, settlement.Items[0].FeeAmount);
    }

    [Fact]
    public async Task SubmitSettlementAsync_RejectsNonAdminMember()
    {
        var adminId = $"c33-st-admin-{Guid.NewGuid():N}";
        var memberId = $"c33-st-member-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        await SeedConfirmationAsync(eventId, memberId, feeAmount: 15m);

        // memberId e jogador confirmado, nao admin do grupo
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PlatformFeeSettlementService>();
        var result = await service.SubmitSettlementAsync(
            groupId, memberId, 15m,
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png",
            new[] { eventId });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReviewSettlementAsync_ApproveAndReject_OnPostgres()
    {
        var adminId = $"c33-rv-admin-{Guid.NewGuid():N}";
        var payerId = $"c33-rv-payer-{Guid.NewGuid():N}";
        var reviewerId = $"c33-reviewer-{Guid.NewGuid():N}";
        await EnsureRoleAsync(reviewerId, "admin");
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var eventId = await SeedEventInGroupAsync(groupId, adminId);
        var payer2Id = $"c33-rv-payer2-{Guid.NewGuid():N}";
        await SeedConfirmationAsync(eventId, payerId, feeAmount: 15m);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PlatformFeeSettlementService>();

        var submit = await service.SubmitSettlementAsync(
            groupId, adminId, 15m,
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png",
            new[] { eventId });
        Assert.True(submit.Success, submit.Message);

        // Reject precisa de motivo — sem note deve falhar
        var rejectNoNote = await service.ReviewSettlementAsync(submit.SettlementId!.Value, reviewerId, approved: false);
        Assert.False(rejectNoNote.Success);

        var approve = await service.ReviewSettlementAsync(submit.SettlementId!.Value, reviewerId, approved: true);
        Assert.True(approve.Success, approve.Message);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settlement = await db.PlatformFeeSettlements.FindAsync(submit.SettlementId.Value);
        Assert.NotNull(settlement);
        Assert.Equal(PlatformFeeSettlementStatus.Pago, settlement.Status);
        Assert.Equal(reviewerId, settlement.ReviewedByUserId);
        Assert.NotNull(settlement.ReviewedAt);
    }

    // ── UC-O-06: aprovar todas as solicitacoes pendentes ─────────────────

    [Fact]
    public async Task ApproveAllPendingAsync_ApprovesAll_AndSendsMailbox()
    {
        var adminId = $"c33-ap-admin-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var requesterIds = new[] { $"c33-ap-r1-{Guid.NewGuid():N}", $"c33-ap-r2-{Guid.NewGuid():N}" };

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var rid in requesterIds)
            {
                await _factory.EnsureUserAsync(rid);
                seedDb.GroupJoinRequests.Add(new GroupJoinRequest
                {
                    GroupId = groupId,
                    UserId = rid,
                    Status = JoinRequestStatus.Pending,
                });
            }
            await seedDb.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<GroupDetailService>();
            await service.ApproveAllPendingAsync(groupId, adminId);
        }

        // Contexto novo: o anterior trackearia as entidades seeded como Pending.
        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var requests = await db.GroupJoinRequests.Where(r => r.GroupId == groupId).ToListAsync();
        Assert.All(requests, r =>
        {
            Assert.Equal(JoinRequestStatus.Approved, r.Status);
            Assert.Equal(adminId, r.RespondedByUserId);
            Assert.NotNull(r.RespondedAt);
        });

        var members = await db.GroupMembers
            .Where(m => m.GroupId == groupId && requesterIds.Contains(m.UserId))
            .ToListAsync();
        Assert.Equal(2, members.Count);
        Assert.All(members, m => Assert.Equal(GroupMemberRole.Member, m.Role));

        var mails = await db.UserMailboxMessages
            .Where(m => requesterIds.Contains(m.RecipientUserId!))
            .ToListAsync();
        Assert.Equal(2, mails.Count);
    }

    [Fact]
    public async Task ApproveAllPendingAsync_LeavesOtherGroupsAlone()
    {
        var adminId = $"c33-ap2-admin-{Guid.NewGuid():N}";
        var otherAdminId = $"c33-ap2-admin2-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var otherGroupId = await _factory.SeedGroupWithAdminAsync(otherAdminId);
        var rid = $"c33-ap2-r-{Guid.NewGuid():N}";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await _factory.EnsureUserAsync(rid);
            seedDb.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = otherGroupId,
                UserId = rid,
                Status = JoinRequestStatus.Pending,
            });
            await seedDb.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<GroupDetailService>();
            await service.ApproveAllPendingAsync(groupId, adminId);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var other = await db.GroupJoinRequests
            .FirstAsync(r => r.GroupId == otherGroupId);
        Assert.Equal(JoinRequestStatus.Pending, other.Status);
    }

    [Fact]
    public async Task ApproveAllPendingAsync_NonAdmin_DoesNotApprove()
    {
        var adminId = $"c33-ap3-admin-{Guid.NewGuid():N}";
        var memberId = $"c33-ap3-member-{Guid.NewGuid():N}";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId);
        var rid = $"c33-ap3-r-{Guid.NewGuid():N}";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await _factory.EnsureUserAsync(memberId);
            seedDb.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = memberId,
                Role = GroupMemberRole.Member,
                CreatedAt = DateTime.UtcNow,
            });
            await _factory.EnsureUserAsync(rid);
            seedDb.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = rid,
                Status = JoinRequestStatus.Pending,
            });
            await seedDb.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<GroupDetailService>();
            await service.ApproveAllPendingAsync(groupId, memberId);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var req = await db.GroupJoinRequests.FirstAsync(r => r.GroupId == groupId && r.UserId == rid);
        Assert.Equal(JoinRequestStatus.Pending, req.Status);
        Assert.Null(req.RespondedByUserId);
        Assert.False(await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == rid));
    }

    // ── UC-J-01 / UC-J-07 / UC-J-08: POSTs de identidade ─────────────────

    [Fact]
    public async Task Register_Post_CreatesUserWithUserRole()
    {
        var email = $"c33-reg-{Guid.NewGuid():N}@test.local";
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetAntiforgeryTokenAsync(client, "/Identity/Account/Register");
        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("__RequestVerificationToken", token),
            KeyValuePair.Create("Input.Email", email),
            KeyValuePair.Create("Input.Password", "TestPass123!"),
            KeyValuePair.Create("Input.ConfirmPassword", "TestPass123!"),
        });

        var response = await client.PostAsync("/Identity/Account/Register", form);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Register POST returned {(int)response.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await userManager.IsInRoleAsync(user, "user"));
    }

    [Fact]
    public async Task Logout_Post_Redirects()
    {
        var userId = $"c33-logout-{Guid.NewGuid():N}";
        await _factory.EnsureUserAsync(userId);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // OnGet de Logout ja faz sign-out e redireciona — o token antiforgery
        // vem de outra pagina (o par token+cookie e valido para o site inteiro),
        // mas deve ser emitido ja autenticado: o token e bound ao usuario.
        var getResponse = await client.SendAsync(AuthedGet("/Identity/Account/Logout", userId));
        Assert.Equal(HttpStatusCode.Redirect, getResponse.StatusCode);

        var loginPage = await client.SendAsync(AuthedGet("/Identity/Account/Login", userId));
        var token = ExtractAntiforgeryToken(await loginPage.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(token), "Login page did not render an antiforgery token.");

        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("__RequestVerificationToken", token),
        });
        var post = new HttpRequestMessage(HttpMethod.Post, "/Identity/Account/Logout")
        {
            Content = form
        };
        post.Headers.Add("X-Test-UserId", userId);
        var response = await client.SendAsync(post);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Logout POST returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task ChangePassword_Post_ChangesPassword()
    {
        var userId = $"c33-pwd-{Guid.NewGuid():N}";
        var email = $"{userId}@test.local";
        await _factory.EnsureUserAsync(userId, email);

        using (var seedScope = _factory.Services.CreateScope())
        {
            var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            var addPwd = await userManager.AddPasswordAsync(user, "OldPass123!");
            Assert.True(addPwd.Succeeded, string.Join("; ", addPwd.Errors.Select(e => e.Description)));
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var getResponse = await client.SendAsync(AuthedGet("/Identity/Account/ChangePassword", userId));
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        Assert.False(string.IsNullOrEmpty(token), "ChangePassword page did not render an antiforgery token.");

        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("__RequestVerificationToken", token),
            KeyValuePair.Create("Input.CurrentPassword", "OldPass123!"),
            KeyValuePair.Create("Input.NewPassword", "NewPass456!"),
            KeyValuePair.Create("Input.ConfirmPassword", "NewPass456!"),
        });
        var post = new HttpRequestMessage(HttpMethod.Post, "/Identity/Account/ChangePassword")
        {
            Content = form
        };
        post.Headers.Add("X-Test-UserId", userId);
        var response = await client.SendAsync(post);

        using var scope = _factory.Services.CreateScope();
        var userManager2 = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updated = await userManager2.FindByIdAsync(userId);
        Assert.NotNull(updated);
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"ChangePassword POST returned {(int)response.StatusCode}");
        Assert.True(await userManager2.CheckPasswordAsync(updated, "NewPass456!"),
            "Password was not changed to the new value.");
    }

    // ── Antiforgery helpers ──────────────────────────────────────────────

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        return ExtractAntiforgeryToken(html);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["token"].Value : string.Empty;
    }
}
