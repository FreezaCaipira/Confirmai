using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class AdminPaymentsCommandServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, AdminPaymentsCommandService svc) Setup(string? userId = null)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"adminpay-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var gatewayFactory = new EventPaymentGatewayFactory(
            new List<Services.Interfaces.IEventPaymentGateway>(),
            new GatewayService(factory));
        var reconService = new EventPaymentReconciliationService(factory, gatewayFactory, logService);
        var statusService = new EventConfirmationPaymentStatusService(factory, logService);
        var exportService = new AdminLogsExportService();
        var queryService = new AdminPaymentsQueryService(factory, exportService);

        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));

        var svc = new AdminPaymentsCommandService(reconService, statusService, queryService, authMock.Object, new StubJSRuntime());
        return (factory, svc);
    }

    private static async Task<(IDbContextFactory<AppDbContext> factory, AdminPaymentsCommandService svc, int confirmationId)> SetupWithConfirmationAsync()
    {
        var (factory, svc) = Setup("admin-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin" });
        db.Users.Add(new ApplicationUser { Id = "player-1", UserName = "Player" });
        var group = new Group { Id = 1, Name = "Group", Sport = Sport.Futsal };
        db.Groups.Add(group);
        var ev = new Event { Id = 1, GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow, Location = "Loc", MaxPlayers = 10 };
        db.Events.Add(ev);
        var conf = new EventConfirmation
        {
            EventId = 1, UserId = "player-1", PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow, PixTxId = "charge-123"
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        return (factory, svc, conf.Id);
    }

    // ── ReconcileChargeAsync ──────────────────────────────────────────

    [Fact]
    public async Task ReconcileChargeAsync_ReturnsError_WhenChargeIdIsEmpty()
    {
        var (_, svc) = Setup();
        var result = await svc.ReconcileChargeAsync("");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ReconcileChargeAsync_ReturnsError_WhenChargeIdIsWhitespace()
    {
        var (_, svc) = Setup();
        var result = await svc.ReconcileChargeAsync("   ");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ReconcileChargeAsync_ReturnsNotFound_WhenChargeIdDoesNotExist()
    {
        var (_, svc) = Setup("admin-1");
        var result = await svc.ReconcileChargeAsync("nonexistent-charge");
        Assert.True(result.IsError);
        Assert.Null(result.ConfirmationId);
    }

    [Fact]
    public async Task ReconcileChargeAsync_TrimsChargeId()
    {
        var (factory, svc, confId) = await SetupWithConfirmationAsync();
        var result = await svc.ReconcileChargeAsync("  charge-123  ");
        Assert.Equal(confId, result.ConfirmationId);
    }

    // ── RunSweepAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RunSweepAsync_ReturnsNoWorkMessage_WhenNoPendingConfirmations()
    {
        var (_, svc) = Setup("admin-1");
        var result = await svc.RunSweepAsync();
        Assert.False(result.IsError);
        Assert.Contains("Nenhuma", result.Message);
    }

    [Fact]
    public async Task RunSweepAsync_ReturnsMessage_WhenPendingConfirmationsExist()
    {
        var (_, svc, _) = await SetupWithConfirmationAsync();
        var result = await svc.RunSweepAsync();
        Assert.Contains("Varredura", result.Message);
    }

    // ── ApplyStatusTransitionAsync ────────────────────────────────────

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenConfirmationIdIsNull()
    {
        var (_, svc) = Setup();
        var result = await svc.ApplyStatusTransitionAsync(null, "Paid", "");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenConfirmationIdIsZero()
    {
        var (_, svc) = Setup();
        var result = await svc.ApplyStatusTransitionAsync(0, "Paid", "");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenInvalidStatusString()
    {
        var (_, svc) = Setup();
        var result = await svc.ApplyStatusTransitionAsync(1, "InvalidStatus", "");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenConfirmationNotFound()
    {
        var (_, svc) = Setup("admin-1");
        var result = await svc.ApplyStatusTransitionAsync(999, "Paid", "");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_TransitionsToPaid_WhenValid()
    {
        var (factory, svc, confId) = await SetupWithConfirmationAsync();
        var result = await svc.ApplyStatusTransitionAsync(confId, "Paid", "Manual override");
        Assert.False(result.IsError);
        Assert.Equal(confId, result.ConfirmationId);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.FindAsync(confId);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, conf!.PaymentStatus);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenAlreadyInTargetStatus()
    {
        var (factory, svc, confId) = await SetupWithConfirmationAsync();
        await svc.ApplyStatusTransitionAsync(confId, "Paid", "");
        var result = await svc.ApplyStatusTransitionAsync(confId, "Paid", "");
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ReturnsError_WhenInvalidTransition()
    {
        var (factory, svc, confId) = await SetupWithConfirmationAsync();
        var result = await svc.ApplyStatusTransitionAsync(confId, "Refunded", "");
        Assert.True(result.IsError);
        Assert.Contains("inválida", result.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task ApplyStatusTransitionAsync_ParsesStatusCaseInsensitively()
    {
        var (factory, svc, confId) = await SetupWithConfirmationAsync();
        var result = await svc.ApplyStatusTransitionAsync(confId, "paid", "");
        Assert.False(result.IsError);
    }

    // ── ExportReconciliationCsvAsync ──────────────────────────────────

    [Fact]
    public async Task ExportReconciliationCsvAsync_CompletesWithoutError()
    {
        var (_, svc) = Setup("admin-1");
        await svc.ExportReconciliationCsvAsync();
    }
}

internal sealed class StubJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        ValueTask.FromResult<TValue>(default!);
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        ValueTask.FromResult<TValue>(default!);
}
