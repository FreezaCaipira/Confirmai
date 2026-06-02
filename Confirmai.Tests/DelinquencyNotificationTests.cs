using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;

namespace Confirmai.Tests;

public class DelinquencyNotificationTests
{
    private static (AppDbContext db, EventNotificationService svc, Mock<IEmailSender> emailMock)
        Build()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var emailMock = new Mock<IEmailSender>();
        var svc = new EventNotificationService(factory, emailMock.Object);
        return (db, svc, emailMock);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_CreatesMailboxMessage()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m),
            (DateTime.UtcNow.AddDays(-3), 75m)
        };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        var mailbox = db.UserMailboxMessages.Single();
        Assert.Equal("admin-1", mailbox.SenderUserId);
        Assert.Equal("user-1", mailbox.RecipientUserId);
        Assert.Contains("Grupo A", mailbox.Subject);
        Assert.Contains("2 partida", mailbox.Body);
        Assert.Contains("125", mailbox.Body);  // Total amount (flexible format)
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_SendsEmailWhenUserHasEmail()
    {
        // Arrange
        var (db, svc, emailMock) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        emailMock.Verify(
            x => x.SendEmailAsync(
                It.Is<string>(e => e == "joao@example.com"),
                It.Is<string>(s => s.Contains("Grupo A")),
                It.Is<string>(b => b.Contains("partida"))),
            Times.Once);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_SkipsEmailWhenUserHasNoEmail()
    {
        // Arrange
        var (db, svc, emailMock) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = null };  // No email
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        emailMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_HandlesEmailSendingException()
    {
        // Arrange
        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var svc = new EventNotificationService(factory, emailMock.Object);

        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act & Assert - Should not throw
        var result = await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        Assert.Equal("João Silva", result.UserName);
        Assert.Equal("joao@example.com", result.Email);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_ReturnsUserInfo()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser 
        { 
            Id = "user-1", 
            UserName = "joao", 
            FullName = "João Silva", 
            Email = "joao@example.com",
            PhoneNumber = "11999999999"
        };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act
        var (userName, email, phone) = await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        Assert.Equal("João Silva", userName);
        Assert.Equal("joao@example.com", email);
        Assert.Equal("11999999999", phone);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_ReturnsEmptyWhenUserNotFound()
    {
        // Arrange
        var (db, svc, _) = Build();
        var entries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act
        var (userName, email, phone) = await svc.NotifyDelinquencyAsync("admin-1", "user-999", "Grupo A", entries);

        // Assert
        Assert.Empty(userName);
        Assert.Null(email);
        Assert.Null(phone);

        var mailboxCount = db.UserMailboxMessages.Count();
        Assert.Equal(0, mailboxCount);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_FormatsSingleVsMultipleEntries()
    {
        // Arrange - Single entry
        var (db1, svc1, _) = Build();
        var admin1 = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user1 = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db1.Users.Add(admin1);
        db1.Users.Add(user1);
        db1.SaveChanges();

        var singleEntry = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m)
        };

        // Act
        await svc1.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", singleEntry);

        // Assert
        var mailbox1 = db1.UserMailboxMessages.Single();
        Assert.Contains("1 partida sem", mailbox1.Body);  // Singular
        Assert.DoesNotContain("partidas sem", mailbox1.Body);  // Not plural

        // Arrange - Multiple entries
        var (db2, svc2, _) = Build();
        var admin2 = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user2 = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db2.Users.Add(admin2);
        db2.Users.Add(user2);
        db2.SaveChanges();

        var multipleEntries = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-5), 50m),
            (DateTime.UtcNow.AddDays(-3), 75m)
        };

        // Act
        await svc2.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", multipleEntries);

        // Assert
        var mailbox2 = db2.UserMailboxMessages.Single();
        Assert.Contains("2 partidas sem", mailbox2.Body);  // Plural
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_IncludesAllEntries()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)>
        {
            (new DateTime(2026, 05, 25, 18, 30, 0, DateTimeKind.Utc), 50m),
            (new DateTime(2026, 05, 27, 19, 00, 0, DateTimeKind.Utc), 75m),
            (new DateTime(2026, 05, 29, 20, 30, 0, DateTimeKind.Utc), 40m)
        };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        var mailbox = db.UserMailboxMessages.Single();
        Assert.Contains("50", mailbox.Body);   // 50
        Assert.Contains("75", mailbox.Body);   // 75
        Assert.Contains("40", mailbox.Body);   // 40
        Assert.Contains("165", mailbox.Body);  // Total: 50+75+40
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_UsesFullNameWhenAvailable()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser 
        { 
            Id = "user-1", 
            UserName = "joao",
            FullName = "João Silva",
            Email = "joao@example.com" 
        };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };

        // Act
        var (userName, _, _) = await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        Assert.Equal("João Silva", userName);

        var mailbox = db.UserMailboxMessages.Single();
        Assert.Contains("João Silva", mailbox.Body);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_UsesUserNameWhenNoFullName()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser 
        { 
            Id = "user-1", 
            UserName = "joao",
            FullName = null,  // No full name
            Email = "joao@example.com" 
        };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };

        // Act
        var (userName, _, _) = await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert
        Assert.Equal("joao", userName);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_CreatesUniqueMailboxEntries()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };

        // Act - Call twice
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        // Assert - Should have 2 separate mailbox entries
        var mailboxCount = db.UserMailboxMessages.Count();
        Assert.Equal(2, mailboxCount);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_IncludesGroupNameInSubject()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Racha do Zé", entries);

        // Assert
        var mailbox = db.UserMailboxMessages.Single();
        Assert.Contains("Racha do Zé", mailbox.Subject);
        Assert.Contains("[Confirmai]", mailbox.Subject);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_IncludesGroupNameInBody()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Racha do Zé", entries);

        // Assert
        var mailbox = db.UserMailboxMessages.Single();
        Assert.Contains("Racha do Zé", mailbox.Body);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_MailboxTimestampIsUtcNow()
    {
        // Arrange
        var (db, svc, _) = Build();
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin", FullName = "Admin Silva" };
        var user = new ApplicationUser { Id = "user-1", UserName = "joao", FullName = "João Silva", Email = "joao@example.com" };
        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();

        var entries = new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-5), 50m) };
        var beforeCall = DateTime.UtcNow;

        // Act
        await svc.NotifyDelinquencyAsync("admin-1", "user-1", "Grupo A", entries);

        var afterCall = DateTime.UtcNow;

        // Assert
        var mailbox = db.UserMailboxMessages.Single();
        Assert.True(mailbox.CreatedAt >= beforeCall);
        Assert.True(mailbox.CreatedAt <= afterCall);
    }
}
