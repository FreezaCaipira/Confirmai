using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for admin visibility of Pix proofs in delinquency list.
/// Ensures that when a player uploads a proof, the admin can see it in Groups/Detail.razor.
/// </summary>
public class PixProofAdminVisibilityTests
{
    private sealed class TestInMemoryDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;
        public TestInMemoryDbContextFactory(string dbName) => _dbName = dbName;
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new AppDbContext(options);
        }
    }

    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) BuildDb()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return (new AppDbContext(options), new TestInMemoryDbContextFactory(dbName));
    }

    [Fact]
    public async Task AdminSeesProofBadge_WhenPlayerUploadsComprovante()
    {
        // Arrange: Setup group, event, players
        var (db, factory) = BuildDb();
        var group = TestDataFactory.CreateGroup("Racha do Zé");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var jogador1 = new ApplicationUser { Id = "jogador1", UserName = "jogador1", FullName = "Jogador Um" };
        
        var conf = TestDataFactory.CreateEventConfirmation(evt, jogador1);
        
        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(admin);
        db.Users.Add(jogador1);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = admin.Id, Role = GroupMemberRole.Admin });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = jogador1.Id, Role = GroupMemberRole.Member });
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        // Act: Player uploads proof (simulating EventPayment.razor UploadProof)
        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic
        await using var uploadDb = await factory.CreateDbContextAsync();
        var entity = await uploadDb.EventConfirmations.FindAsync(conf.Id);
        if (entity is not null)
        {
            entity.PixProofImageData = proofBytes;
            entity.PixProofContentType = "image/jpeg";
            entity.PixProofUploadedAt = DateTime.UtcNow;
            await uploadDb.SaveChangesAsync();
        }

        // Act: Admin loads delinquency list (simulating Groups/Detail.razor LoadDelinquencyData)
        await using var adminDb = await factory.CreateDbContextAsync();
        var memberIds = await adminDb.GroupMembers
            .Where(m => m.GroupId == group.Id)
            .Select(m => m.UserId)
            .ToListAsync();

        var unpaidConfirmations = await adminDb.EventConfirmations
            .Where(c =>
                c.Event.GroupId == group.Id &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderBy(c => c.Event.StartsAt)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.EventId,
                c.Event,
                c.User,
                c.PaymentGatewayName,
                c.Position,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        var delinquencyList = unpaidConfirmations
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var userName = user?.FullName ?? user?.UserName ?? "Jogador";
                var entries = g.Select(c =>
                {
                    var href = $"/futsal/{c.EventId}";
                    return new DelinquencyEntry(c.Id, c.EventId, c.Event.StartsAt, c.Event.Price!.Value, href, c.HasProof);
                }).ToList();
                return new UserDelinquency(g.Key, userName, entries);
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();

        // Assert: Admin should see the delinquency entry with proof indicator
        Assert.NotEmpty(delinquencyList);
        var delinquent = delinquencyList.Single();
        Assert.Equal("jogador1", delinquent.UserId);
        Assert.Equal("Jogador Um", delinquent.UserName);
        Assert.Single(delinquent.Entries);

        var entry = delinquent.Entries.Single();
        Assert.Equal(conf.Id, entry.ConfirmationId);
        Assert.Equal(50m, entry.EventPrice);
        Assert.True(entry.HasProof, "Admin should see that this entry has a proof uploaded");
    }

    [Fact]
    public async Task AdminSeesProofBadge_MultiplePlayersWithMixedProofs()
    {
        // Arrange: 3 players, 2 uploaded proofs, 1 didn't
        var (db, factory) = BuildDb();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin" };
        var jogador1 = new ApplicationUser { Id = "jogador1", UserName = "jogador1", FullName = "Jogador 1" };
        var jogador2 = new ApplicationUser { Id = "jogador2", UserName = "jogador2", FullName = "Jogador 2" };
        var jogador3 = new ApplicationUser { Id = "jogador3", UserName = "jogador3", FullName = "Jogador 3" };

        var conf1 = TestDataFactory.CreateEventConfirmation(evt, jogador1);
        var conf2 = TestDataFactory.CreateEventConfirmation(evt, jogador2);
        var conf3 = TestDataFactory.CreateEventConfirmation(evt, jogador3);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.AddRange(admin, jogador1, jogador2, jogador3);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = admin.Id, Role = GroupMemberRole.Admin });
        db.GroupMembers.AddRange(
            new GroupMember { GroupId = group.Id, UserId = jogador1.Id, Role = GroupMemberRole.Member },
            new GroupMember { GroupId = group.Id, UserId = jogador2.Id, Role = GroupMemberRole.Member },
            new GroupMember { GroupId = group.Id, UserId = jogador3.Id, Role = GroupMemberRole.Member }
        );
        db.EventConfirmations.AddRange(conf1, conf2, conf3);
        await db.SaveChangesAsync();

        // Act: Only jogador1 and jogador3 upload proofs
        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        await using var uploadDb = await factory.CreateDbContextAsync();
        
        var entity1 = await uploadDb.EventConfirmations.FindAsync(conf1.Id);
        if (entity1 is not null)
        {
            entity1.PixProofUploadedAt = DateTime.UtcNow;
            entity1.PixProofImageData = proofBytes;
        }

        var entity3 = await uploadDb.EventConfirmations.FindAsync(conf3.Id);
        if (entity3 is not null)
        {
            entity3.PixProofUploadedAt = DateTime.UtcNow;
            entity3.PixProofImageData = proofBytes;
        }

        await uploadDb.SaveChangesAsync();

        // Act: Admin loads delinquency
        await using var adminDb = await factory.CreateDbContextAsync();
        var memberIds = await adminDb.GroupMembers
            .Where(m => m.GroupId == group.Id)
            .Select(m => m.UserId)
            .ToListAsync();

        var unpaidConfirmations = await adminDb.EventConfirmations
            .Where(c =>
                c.Event.GroupId == group.Id &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderBy(c => c.Event.StartsAt)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.EventId,
                c.Event,
                c.User,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        var delinquencyList = unpaidConfirmations
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var userName = user?.FullName ?? user?.UserName ?? "Jogador";
                var entries = g.Select(c =>
                {
                    return new DelinquencyEntry(c.Id, c.EventId, c.Event.StartsAt, c.Event.Price!.Value, 
                        $"/futsal/{c.EventId}", c.HasProof);
                }).ToList();
                return new UserDelinquency(g.Key, userName, entries);
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();

        // Assert: All 3 should appear in delinquency list
        Assert.Equal(3, delinquencyList.Count);

        var del1 = delinquencyList.Single(d => d.UserId == "jogador1");
        var del2 = delinquencyList.Single(d => d.UserId == "jogador2");
        var del3 = delinquencyList.Single(d => d.UserId == "jogador3");

        // Only jogador1 and jogador3 have proofs
        Assert.True(del1.Entries.All(e => e.HasProof), "Jogador 1 should have proof");
        Assert.False(del2.Entries.Any(e => e.HasProof), "Jogador 2 should NOT have proof");
        Assert.True(del3.Entries.All(e => e.HasProof), "Jogador 3 should have proof");

        // Check badge visibility condition
        Assert.True(del1.Entries.Any(e => e.HasProof), "Badge should be visible for jogador1");
        Assert.False(del2.Entries.Any(e => e.HasProof), "Badge should NOT be visible for jogador2");
        Assert.True(del3.Entries.Any(e => e.HasProof), "Badge should be visible for jogador3");
    }

    [Fact]
    public async Task AdminCanViewProofImage_AfterPlayerUploads()
    {
        // Arrange
        var (db, factory) = BuildDb();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var jogador1 = new ApplicationUser { Id = "jogador1", UserName = "jogador1", FullName = "Jogador 1" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, jogador1);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(jogador1);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        // Act: Player uploads proof
        var proofBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic
        await using var uploadDb = await factory.CreateDbContextAsync();
        var entity = await uploadDb.EventConfirmations.FindAsync(conf.Id);
        if (entity is not null)
        {
            entity.PixProofImageData = proofBytes;
            entity.PixProofContentType = "image/png";
            entity.PixProofUploadedAt = DateTime.UtcNow;
            await uploadDb.SaveChangesAsync();
        }

        // Act: Admin queries the proof image
        await using var viewDb = await factory.CreateDbContextAsync();
        var proofConf = await viewDb.EventConfirmations.FindAsync(conf.Id);

        // Assert: Admin can retrieve the image
        Assert.NotNull(proofConf);
        Assert.NotNull(proofConf.PixProofImageData);
        Assert.Equal(proofBytes, proofConf.PixProofImageData);
        Assert.Equal("image/png", proofConf.PixProofContentType);
    }

    [Fact]
    public async Task AdminNotSeesProof_WhenPlayerNeverUploaded()
    {
        // Arrange
        var (db, factory) = BuildDb();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var jogador1 = new ApplicationUser { Id = "jogador1", UserName = "jogador1", FullName = "Jogador 1" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, jogador1);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(jogador1);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = jogador1.Id, Role = GroupMemberRole.Member });
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        // Act: Load delinquency WITHOUT upload
        await using var adminDb = await factory.CreateDbContextAsync();
        var memberIds = await adminDb.GroupMembers
            .Where(m => m.GroupId == group.Id)
            .Select(m => m.UserId)
            .ToListAsync();

        var unpaidConfirmations = await adminDb.EventConfirmations
            .Where(c =>
                c.Event.GroupId == group.Id &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.EventId,
                c.Event,
                c.User,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        // Assert
        Assert.NotEmpty(unpaidConfirmations);
        Assert.All(unpaidConfirmations, u => Assert.False(u.HasProof));
    }
}
