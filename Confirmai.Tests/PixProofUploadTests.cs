using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confirmai.Tests;

public class PixProofUploadTests
{
    private static (AppDbContext db, PixProofUploadService svc, IDbContextFactory<AppDbContext> factory) Build()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);
        var svc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance);
        return (db, svc, factory);
    }

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

    [Fact]
    public async Task UploadProofAsync_SucceedsWithValidJpg()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic number

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/jpeg");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("sucesso", result.Message);
        Assert.NotNull(result.UploadedAt);

        await using var verifyDb = factory.CreateDbContext();
        var updated = verifyDb.EventConfirmations.Single();
        Assert.NotNull(updated.PixProofImageData);
        Assert.Equal(fileBytes, updated.PixProofImageData);
        Assert.Equal("image/jpeg", updated.PixProofContentType);
        Assert.NotNull(updated.PixProofUploadedAt);
    }

    [Fact]
    public async Task UploadProofAsync_SucceedsWithValidPng()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic number

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/png");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        Assert.Equal("image/png", verifyDb.EventConfirmations.Single().PixProofContentType);
    }

    [Fact]
    public async Task UploadProofAsync_SucceedsWithValidWebp()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // WEBP magic number

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/webp");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        Assert.Equal("image/webp", verifyDb.EventConfirmations.Single().PixProofContentType);
    }

    [Fact]
    public async Task UploadProofAsync_RejectsInvalidMimeType()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "application/pdf");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("JPG", result.Message);
        await using var verifyDb = factory.CreateDbContext();
        Assert.Null(verifyDb.EventConfirmations.Single().PixProofImageData);
    }

    [Fact]
    public async Task UploadProofAsync_RejectsEmptyMimeType()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("JPG", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_RejectsEmptyFile()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        // Act
        var result = await svc.UploadProofAsync(confId, new byte[0], "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("vazio", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_RejectsNullFile()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        // Act
        var result = await svc.UploadProofAsync(confId, null!, "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("vazio", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_RejectsFileTooLarge()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        // Create 6MB file (exceeds 5MB limit)
        var fileBytes = new byte[6 * 1024 * 1024];
        Array.Fill(fileBytes, (byte)0xFF);

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("muito grande", result.Message);
        await using var verifyDb = factory.CreateDbContext();
        Assert.Null(verifyDb.EventConfirmations.Single().PixProofImageData);
    }

    [Fact]
    public async Task UploadProofAsync_Accepts5MbExactly()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        // Create exactly 5MB file
        var fileBytes = new byte[5 * 1024 * 1024];
        Array.Fill(fileBytes, (byte)0xFF);

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/jpeg");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        Assert.NotNull(verifyDb.EventConfirmations.Single().PixProofImageData);
    }

    [Fact]
    public async Task UploadProofAsync_ReturnsErrorWhenConfirmationNotFound()
    {
        // Arrange
        var (_, svc, _) = Build();

        // Act
        var result = await svc.UploadProofAsync(999, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("não encontrada", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_UpdatesOnlyTargetConfirmation()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user1 = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var user2 = new ApplicationUser { Id = "user-2", UserName = "maria", FullName = "Maria Silva" };
        var conf1 = TestDataFactory.CreateEventConfirmation(evt, user1);
        var conf2 = TestDataFactory.CreateEventConfirmation(evt, user2);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user1);
        db.Users.Add(user2);
        db.EventConfirmations.AddRange(conf1, conf2);
        await db.SaveChangesAsync();
        var confId1 = conf1.Id;
        var confId2 = conf2.Id;

        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act
        var result = await svc.UploadProofAsync(confId1, fileBytes, "image/jpeg");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        var updated1 = verifyDb.EventConfirmations.Find(confId1);
        var updated2 = verifyDb.EventConfirmations.Find(confId2);

        Assert.NotNull(updated1!.PixProofImageData);
        Assert.Null(updated2!.PixProofImageData);
    }

    [Fact]
    public async Task UploadProofAsync_SetsCorrectTimestamp()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "image/jpeg");

        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        var updated = verifyDb.EventConfirmations.Single();
        Assert.NotNull(updated.PixProofUploadedAt);
        Assert.True(updated.PixProofUploadedAt >= beforeCall);
        Assert.True(updated.PixProofUploadedAt <= afterCall);
        Assert.Equal(updated.PixProofUploadedAt, result.UploadedAt);
    }

    [Fact]
    public async Task UploadProofAsync_ReplacesExistingProof()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var oldFileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var newFileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Upload first proof
        await svc.UploadProofAsync(confId, oldFileBytes, "image/jpeg");
        await using var verifyDb1 = factory.CreateDbContext();
        var firstUpload = verifyDb1.EventConfirmations.Single().PixProofUploadedAt;

        await Task.Delay(10);  // Small delay to ensure different timestamp

        // Act - Upload second proof
        var result = await svc.UploadProofAsync(confId, newFileBytes, "image/png");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb2 = factory.CreateDbContext();
        var updated = verifyDb2.EventConfirmations.Single();
        Assert.Equal(newFileBytes, updated.PixProofImageData);
        Assert.Equal("image/png", updated.PixProofContentType);
        Assert.True(updated.PixProofUploadedAt > firstUpload);
    }

    [Fact]
    public async Task UploadProofAsync_HandlesMimeTypeCaseInsensitive()
    {
        // Arrange
        var (db, svc, factory) = Build();
        var group = TestDataFactory.CreateGroup("Racha A");
        var evt = TestDataFactory.CreateEvent(group, "2026-06-15 18:00", price: 50m);
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva" };
        var conf = TestDataFactory.CreateEventConfirmation(evt, user);

        db.Groups.Add(group);
        db.Events.Add(evt);
        db.Users.Add(user);
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confId = conf.Id;

        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act
        var result = await svc.UploadProofAsync(confId, fileBytes, "IMAGE/JPEG");

        // Assert
        Assert.True(result.Success);
        await using var verifyDb = factory.CreateDbContext();
        Assert.NotNull(verifyDb.EventConfirmations.Single().PixProofImageData);
    }
}
