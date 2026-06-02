using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// End-to-end tests for the manual Pix payment flow: user uploads proof → admin confirms payment.
/// </summary>
public class PixManualPaymentFlowTests
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

    private static (AppDbContext db, PixProofUploadService uploadSvc, AdminConfirmationService confirmSvc, IDbContextFactory<AppDbContext> factory) BuildServices()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);
        var uploadSvc = new PixProofUploadService(factory);
        
        var logService = new LogService(db, NullLogger<LogService>.Instance);
        var confirmSvc = new AdminConfirmationService(factory, logService);
        
        return (db, uploadSvc, confirmSvc, factory);
    }

    [Fact]
    public async Task PixManualPaymentFlow_UserUploadsProof_AdminConfirmsPayment()
    {
        // Arrange
        var (db, uploadSvc, confirmSvc, factory) = BuildServices();

        // Setup: Create event, group, users, confirmation
        var group = TestDataFactory.CreateGroup("Racha do Zé");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var payer = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, payer);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(payer);
        db.Users.Add(admin);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var confId = conf.Id;
        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic number

        // Act 1: User uploads Pix proof
        var uploadResult = await uploadSvc.UploadProofAsync(confId, proofBytes, "image/jpeg");

        // Assert 1: Proof is saved, payment status is still pending
        Assert.True(uploadResult.Success);
        await using var verifyDb1 = factory.CreateDbContext();
        var conf1 = verifyDb1.EventConfirmations.Single();
        Assert.NotNull(conf1.PixProofImageData);
        Assert.Equal("image/jpeg", conf1.PixProofContentType);
        Assert.NotNull(conf1.PixProofUploadedAt);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, conf1.PaymentStatus);
        Assert.False(conf1.HasPaid);
        Assert.Null(conf1.MarkedPaidByUserId);

        // Act 2: Admin confirms the payment after reviewing proof
        var confirmResult = await confirmSvc.TogglePaidAsync(confId, "admin-1");

        // Assert 2: Payment is now marked as paid with admin metadata
        Assert.True(confirmResult.Found);
        Assert.True(confirmResult.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, confirmResult.NewStatus);

        await using var verifyDb2 = factory.CreateDbContext();
        var conf2 = verifyDb2.EventConfirmations.Single();
        
        // Proof should still be there
        Assert.NotNull(conf2.PixProofImageData);
        Assert.Equal(proofBytes, conf2.PixProofImageData);
        Assert.Equal("image/jpeg", conf2.PixProofContentType);
        
        // Payment metadata should be set
        Assert.Equal(EventConfirmationPaymentStatus.Paid, conf2.PaymentStatus);
        Assert.True(conf2.HasPaid);
        Assert.Equal("admin-1", conf2.MarkedPaidByUserId);
        Assert.NotNull(conf2.MarkedPaidAt);
    }

    [Fact]
    public async Task PixManualPaymentFlow_MultiplePayersUploadProofs_AdminConfirmsSelectively()
    {
        // Arrange
        var (db, uploadSvc, confirmSvc, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var payer1 = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var payer2 = new ApplicationUser { Id = "payer-2", UserName = "maria", FullName = "Maria Silva" };
        var payer3 = new ApplicationUser { Id = "payer-3", UserName = "pedro", FullName = "Pedro Silva" };

        var conf1 = TestDataFactory.CreateEventConfirmation(evt, payer1);
        var conf2 = TestDataFactory.CreateEventConfirmation(evt, payer2);
        var conf3 = TestDataFactory.CreateEventConfirmation(evt, payer3);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.AddRange(admin, payer1, payer2, payer3);
        db.EventConfirmations.AddRange(conf1, conf2, conf3);
        await db.SaveChangesAsync();

        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act 1: All three payers upload proofs
        await uploadSvc.UploadProofAsync(conf1.Id, proofBytes, "image/jpeg");
        await uploadSvc.UploadProofAsync(conf2.Id, proofBytes, "image/png");
        await uploadSvc.UploadProofAsync(conf3.Id, proofBytes, "image/webp");

        // Act 2: Admin confirms only payer 1 and 3 (rejects payer 2)
        await confirmSvc.TogglePaidAsync(conf1.Id, "admin-1");
        // Skip payer 2
        await confirmSvc.TogglePaidAsync(conf3.Id, "admin-1");

        // Assert: Only payer 1 and 3 are marked as paid
        await using var verifyDb = factory.CreateDbContext();
        var updated1 = verifyDb.EventConfirmations.Find(conf1.Id);
        var updated2 = verifyDb.EventConfirmations.Find(conf2.Id);
        var updated3 = verifyDb.EventConfirmations.Find(conf3.Id);

        // Payer 1: paid
        Assert.True(updated1!.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updated1.PaymentStatus);
        Assert.Equal("admin-1", updated1.MarkedPaidByUserId);
        Assert.NotNull(updated1.PixProofImageData);

        // Payer 2: still pending
        Assert.False(updated2!.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, updated2.PaymentStatus);
        Assert.Null(updated2.MarkedPaidByUserId);
        Assert.NotNull(updated2.PixProofImageData); // Proof is there but not confirmed

        // Payer 3: paid
        Assert.True(updated3!.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updated3.PaymentStatus);
        Assert.Equal("admin-1", updated3.MarkedPaidByUserId);
        Assert.NotNull(updated3.PixProofImageData);
    }

    [Fact]
    public async Task PixManualPaymentFlow_UploadedProofWithoutAdminConfirmation_RemainsInPendingStatus()
    {
        // Arrange
        var (db, uploadSvc, _, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var payer = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, payer);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(payer);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var confId = conf.Id;
        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act: User uploads proof but admin doesn't confirm yet
        await uploadSvc.UploadProofAsync(confId, proofBytes, "image/jpeg");

        // Assert: Proof is saved but payment status remains pending
        await using var verifyDb = factory.CreateDbContext();
        var updated = verifyDb.EventConfirmations.Single();
        
        Assert.NotNull(updated.PixProofImageData);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, updated.PaymentStatus);
        Assert.False(updated.HasPaid);
        Assert.Null(updated.MarkedPaidByUserId);
        Assert.Null(updated.MarkedPaidAt);
    }

    [Fact]
    public async Task PixManualPaymentFlow_AdminCanToggleBackFromConfirmed()
    {
        // Arrange
        var (db, uploadSvc, confirmSvc, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var payer = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, payer);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(admin);
        db.Users.Add(payer);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var confId = conf.Id;
        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act 1: Upload proof and confirm payment
        await uploadSvc.UploadProofAsync(confId, proofBytes, "image/jpeg");
        var toggleResult1 = await confirmSvc.TogglePaidAsync(confId, "admin-1");
        Assert.True(toggleResult1.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, toggleResult1.NewStatus);

        // Act 2: Admin changes their mind and toggles back to pending
        var toggleResult2 = await confirmSvc.TogglePaidAsync(confId, "admin-1");
        Assert.True(toggleResult2.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, toggleResult2.NewStatus);

        // Assert: Back to pending but proof is still there
        await using var verifyDb = factory.CreateDbContext();
        var updated = verifyDb.EventConfirmations.Single();
        
        Assert.NotNull(updated.PixProofImageData);
        Assert.Equal(proofBytes, updated.PixProofImageData);
        Assert.False(updated.HasPaid);
        Assert.Null(updated.MarkedPaidByUserId);
        Assert.Null(updated.MarkedPaidAt);
    }

    [Fact]
    public async Task PixManualPaymentFlow_ReplacedProofDoesNotAffectConfirmedStatus()
    {
        // Arrange
        var (db, uploadSvc, confirmSvc, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var payer = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, payer);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(admin);
        db.Users.Add(payer);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var confId = conf.Id;
        var proof1 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG
        var proof2 = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG

        // Act 1: Upload proof and confirm
        await uploadSvc.UploadProofAsync(confId, proof1, "image/jpeg");
        var confirmResult = await confirmSvc.TogglePaidAsync(confId, "admin-1");
        Assert.True(confirmResult.Updated);

        await using var verifyDb1 = factory.CreateDbContext();
        var markedAt = verifyDb1.EventConfirmations.Single().MarkedPaidAt;

        await Task.Delay(50); // Ensure timestamp difference

        // Act 2: Payer replaces with better quality proof
        await uploadSvc.UploadProofAsync(confId, proof2, "image/png");

        // Assert: Payment status unchanged, proof updated, timestamp preserved
        await using var verifyDb2 = factory.CreateDbContext();
        var updated = verifyDb2.EventConfirmations.Single();
        
        // Status should remain paid (not reset by new proof)
        Assert.True(updated.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updated.PaymentStatus);
        
        // Proof should be replaced
        Assert.Equal(proof2, updated.PixProofImageData);
        Assert.Equal("image/png", updated.PixProofContentType);
        
        // Admin metadata should remain
        Assert.Equal("admin-1", updated.MarkedPaidByUserId);
        Assert.Equal(markedAt, updated.MarkedPaidAt);
    }

    [Fact]
    public async Task PixManualPaymentFlow_FailedProofUploadDoesNotMutateState()
    {
        // Arrange
        var (db, uploadSvc, _, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var payer = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, payer);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(payer);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var confId = conf.Id;
        var tooLargeFile = new byte[6 * 1024 * 1024]; // 6MB, exceeds limit
        Array.Fill(tooLargeFile, (byte)0xFF);

        // Act: Attempt to upload oversized file
        var uploadResult = await uploadSvc.UploadProofAsync(confId, tooLargeFile, "image/jpeg");

        // Assert: Upload failed and state is unchanged
        Assert.False(uploadResult.Success);
        Assert.Contains("muito grande", uploadResult.Message);

        await using var verifyDb = factory.CreateDbContext();
        var unchanged = verifyDb.EventConfirmations.Single();
        
        Assert.Null(unchanged.PixProofImageData);
        Assert.Null(unchanged.PixProofContentType);
        Assert.Null(unchanged.PixProofUploadedAt);
        Assert.False(unchanged.HasPaid);
    }

    [Fact]
    public async Task PixManualPaymentFlow_BothNonManualAndManualPathsCoexist()
    {
        // Arrange: One user pays via gateway, another via manual Pix
        var (db, uploadSvc, confirmSvc, factory) = BuildServices();

        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var payer1 = new ApplicationUser { Id = "payer-1", UserName = "joao", FullName = "João Silva" };
        var payer2 = new ApplicationUser { Id = "payer-2", UserName = "maria", FullName = "Maria Silva" };

        var conf1 = TestDataFactory.CreateEventConfirmation(evt, payer1);
        var conf2 = TestDataFactory.CreateEventConfirmation(evt, payer2);

        // Payer 1 paid via gateway
        conf1.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf1.HasPaid = true;
        conf1.PaymentGatewayName = "EfiBank";
        conf1.PixTxId = "abc123def456";

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.AddRange(admin, payer1, payer2);
        db.EventConfirmations.AddRange(conf1, conf2);
        await db.SaveChangesAsync();

        var proofBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act: Payer 2 uploads manual Pix proof
        var uploadResult = await uploadSvc.UploadProofAsync(conf2.Id, proofBytes, "image/jpeg");
        Assert.True(uploadResult.Success);

        // Act: Admin confirms manual Pix
        var confirmResult = await confirmSvc.TogglePaidAsync(conf2.Id, "admin-1");
        Assert.True(confirmResult.Updated);

        // Assert: Payer 1 remains unchanged (gateway path), Payer 2 is now confirmed (manual path)
        await using var verifyDb = factory.CreateDbContext();
        var updated1 = verifyDb.EventConfirmations.Find(conf1.Id);
        var updated2 = verifyDb.EventConfirmations.Find(conf2.Id);

        // Gateway path unchanged
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updated1!.PaymentStatus);
        Assert.Equal("EfiBank", updated1.PaymentGatewayName);
        Assert.Null(updated1.MarkedPaidByUserId);

        // Manual path confirmed
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updated2!.PaymentStatus);
        Assert.Equal("admin-1", updated2.MarkedPaidByUserId);
        Assert.NotNull(updated2.PixProofImageData);
    }
}
