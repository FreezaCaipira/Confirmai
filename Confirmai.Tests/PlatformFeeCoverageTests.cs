using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Direct tests for PlatformFeeCoverage, extracted from the ledger in C29 Fase B.
/// </summary>
public class PlatformFeeCoverageTests
{
    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) Setup()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);

        var factory = new TestFactory(dbName);
        return (db, factory);
    }

    private sealed class TestFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;
        public TestFactory(string dbName) => _dbName = dbName;
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new AppDbContext(options);
        }
    }

    private static async Task<int> SeedGroupAndSettlementAsync(
        AppDbContext db, int groupId, int eventId, decimal feeAmount,
        PlatformFeeSettlementStatus status)
    {
        var settlement = new PlatformFeeSettlement
        {
            GroupId = groupId,
            Amount = feeAmount,
            SubmittedByUserId = "org-1",
            Status = status,
            Items = new List<PlatformFeeSettlementItem>
            {
                new() { EventId = eventId, FeeAmount = feeAmount }
            }
        };
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();
        return settlement.Id;
    }

    [Fact]
    public async Task GetCoveredByEventAsync_NoSettlements_ReturnsEmpty()
    {
        var (db, _) = Setup();
        var result = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 1);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCoveredByEventAsync_OnlyApprovedSettlementsAreCounted()
    {
        var (db, _) = Setup();
        // Approved settlement: should count
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Pago);
        // In-review settlement: should NOT count
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.EmAnalise);
        // Rejected settlement: should NOT count
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Rejeitado);

        var result = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 1);

        Assert.Single(result);
        Assert.Equal(0.75m, result[10]);
    }

    [Fact]
    public async Task GetCoveredByEventAsync_SumsMultipleApprovedSettlementsForSameEvent()
    {
        var (db, _) = Setup();
        // First approved settlement covers 0.75 of event 10
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Pago);
        // Late payer: second approved settlement covers another 0.75 of event 10
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Pago);

        var result = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 1);

        Assert.Single(result);
        Assert.Equal(1.50m, result[10]);
    }

    [Fact]
    public async Task GetCoveredByEventAsync_SeparatesByGroup()
    {
        var (db, _) = Setup();
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Pago);
        await SeedGroupAndSettlementAsync(db, 2, 10, 0.50m, PlatformFeeSettlementStatus.Pago);

        var resultGroup1 = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 1);
        var resultGroup2 = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 2);

        Assert.Equal(0.75m, resultGroup1[10]);
        Assert.Equal(0.50m, resultGroup2[10]);
    }

    [Fact]
    public async Task GetCoveredByEventAsync_SeparatesByEvent()
    {
        var (db, _) = Setup();
        await SeedGroupAndSettlementAsync(db, 1, 10, 0.75m, PlatformFeeSettlementStatus.Pago);
        await SeedGroupAndSettlementAsync(db, 1, 20, 1.50m, PlatformFeeSettlementStatus.Pago);

        var result = await PlatformFeeCoverage.GetCoveredByEventAsync(db, 1);

        Assert.Equal(2, result.Count);
        Assert.Equal(0.75m, result[10]);
        Assert.Equal(1.50m, result[20]);
    }
}
