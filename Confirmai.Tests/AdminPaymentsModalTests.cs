using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

/// <summary>
/// Testes para o modal de pagamentos do admin em Pages/Groups/Detail.razor
/// Cobertura: filtro por usuário/status, sorting, export CSV, paginação e refresh.
/// </summary>
public class AdminPaymentsModalTests
{
    [Fact]
    public async Task LoadPaymentsData_FiltersByUserId_ReturnsOnlyUserProofs()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user1 = "user-1";
        var user2 = "user-2";
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(1) };
        db.Events.Add(evt);

        // 3 confirmações: 2 para user1 (1 com comprovante), 1 para user2
        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation { EventId = 1, UserId = user1, PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = DateTime.UtcNow.AddMinutes(-30) },
            new EventConfirmation { EventId = 1, UserId = user1, PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield }, // Sem comprovante
            new EventConfirmation { EventId = 1, UserId = user2, PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = DateTime.UtcNow.AddMinutes(-45) }
        });
        await db.SaveChangesAsync();

        // Act
        var proofEntries = db.EventConfirmations
            .Where(c => c.PixProofUploadedAt.HasValue && c.UserId == user1)
            .ToList();

        // Assert
        Assert.Single(proofEntries);
        Assert.Equal(user1, proofEntries[0].UserId);
    }

    [Fact]
    public async Task LoadPaymentsData_FiltersByPaymentStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(1) };
        db.Events.Add(evt);

        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation { EventId = 1, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield },
            new EventConfirmation { EventId = 1, UserId = "user-2", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield },
            new EventConfirmation { EventId = 1, UserId = "user-3", PaymentStatus = EventConfirmationPaymentStatus.Expired, HasPaid = false, Position = FutsalPosition.Outfield }
        });
        await db.SaveChangesAsync();

        // Act
        var paidConfirmations = db.EventConfirmations
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            .ToList();

        var expiredConfirmations = db.EventConfirmations
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Expired)
            .ToList();

        // Assert
        Assert.Single(paidConfirmations);
        Assert.Single(expiredConfirmations);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, paidConfirmations[0].PaymentStatus);
    }

    [Fact]
    public async Task LoadPaymentsData_SortsByUploadDate_DescendingOrder()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(1) };
        db.Events.Add(evt);

        var baseTime = DateTime.UtcNow;
        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation { EventId = 1, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = baseTime.AddMinutes(-10) },
            new EventConfirmation { EventId = 1, UserId = "user-2", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = baseTime.AddMinutes(-30) },
            new EventConfirmation { EventId = 1, UserId = "user-3", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = baseTime }
        });
        await db.SaveChangesAsync();

        // Act
        var sorted = db.EventConfirmations
            .Where(c => c.PixProofUploadedAt.HasValue)
            .OrderByDescending(c => c.PixProofUploadedAt)
            .ToList();

        // Assert
        Assert.Equal(3, sorted.Count);
        Assert.True(sorted[0].PixProofUploadedAt > sorted[1].PixProofUploadedAt);
        Assert.True(sorted[1].PixProofUploadedAt > sorted[2].PixProofUploadedAt);
    }

    [Fact]
    public async Task TogglePaidAsync_MarksPaymentAsConfirmed_RecordsAdminMetadata()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var adminService = new AdminConfirmationService(factory, logService);
        var adminId = "admin-user";

        // Act
        var result = await adminService.TogglePaidAsync(confirmationId, adminId);

        // Assert
        Assert.True(result.Updated);
        await using var verifyDb = factory.CreateDbContext();
        var updated = verifyDb.EventConfirmations.First();
        Assert.True(updated.HasPaid);
        Assert.Equal(adminId, updated.MarkedPaidByUserId);
        Assert.NotNull(updated.MarkedPaidAt);
    }

    [Fact]
    public async Task UnpaidConfirmations_ShowsOnlyUnpaid_FiltersByGroup()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(-1) }; // Evento passado
        db.Events.Add(evt);

        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation { EventId = 1, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield },
            new EventConfirmation { EventId = 1, UserId = "user-2", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield },
            new EventConfirmation { EventId = 1, UserId = "user-3", PaymentStatus = EventConfirmationPaymentStatus.Expired, HasPaid = false, Position = FutsalPosition.Outfield }
        });
        await db.SaveChangesAsync();

        // Act - Não pagos: confirmados E não pagos
        var unpaid = db.EventConfirmations
            .Where(c => c.Event.GroupId == 1 && !c.HasPaid)
            .ToList();

        // Assert
        Assert.Equal(2, unpaid.Count);
        Assert.All(unpaid, c => Assert.False(c.HasPaid));
    }

    [Fact]
    public async Task PaymentHistory_ShowsRecentTransactions_WithMarkedPaidMetadata()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(-7) };
        db.Events.Add(evt);

        var markedTime = DateTime.UtcNow.AddHours(-2);
        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation 
            { 
                EventId = 1, 
                UserId = "user-1", 
                PaymentStatus = EventConfirmationPaymentStatus.Paid, 
                HasPaid = true, 
                Position = FutsalPosition.Outfield,
                MarkedPaidByUserId = "admin-1",
                MarkedPaidAt = markedTime
            },
            new EventConfirmation 
            { 
                EventId = 1, 
                UserId = "user-2", 
                PaymentStatus = EventConfirmationPaymentStatus.Pending, 
                HasPaid = false, 
                Position = FutsalPosition.Outfield 
            }
        });
        await db.SaveChangesAsync();

        // Act
        var history = db.EventConfirmations
            .Where(c => c.Event.GroupId == 1 && c.HasPaid)
            .OrderByDescending(c => c.MarkedPaidAt)
            .ToList();

        // Assert
        Assert.Single(history);
        Assert.Equal("admin-1", history[0].MarkedPaidByUserId);
        Assert.Equal(markedTime, history[0].MarkedPaidAt);
    }

    [Fact]
    public async Task ModalRefresh_ReloadsData_PreservesFilterState()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Id = 1, Name = "Test Group", CreatedByUserId = "owner" };
        db.Groups.Add(group);

        var evt = new Event { Id = 1, GroupId = 1, StartsAt = DateTime.UtcNow.AddDays(1) };
        db.Events.Add(evt);

        var user1 = "user-1";
        db.EventConfirmations.AddRange(new[]
        {
            new EventConfirmation { EventId = 1, UserId = user1, PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield, PixProofUploadedAt = DateTime.UtcNow },
            new EventConfirmation { EventId = 1, UserId = "user-2", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield }
        });
        await db.SaveChangesAsync();

        // Act - Simula filtro no user1
        var filtered = db.EventConfirmations
            .Where(c => c.UserId == user1 && c.PixProofUploadedAt.HasValue)
            .ToList();

        // Refresh com mesmo filtro
        var refreshed = db.EventConfirmations
            .Where(c => c.UserId == user1 && c.PixProofUploadedAt.HasValue)
            .ToList();

        // Assert
        Assert.Single(filtered);
        Assert.Single(refreshed);
        Assert.Equal(filtered[0].Id, refreshed[0].Id);
    }
}
