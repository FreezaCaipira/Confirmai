using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class PixProofUploadServiceTests
{
    private readonly Mock<ILogger<PixProofUploadService>> _loggerMock;

    public PixProofUploadServiceTests()
    {
        _loggerMock = new Mock<ILogger<PixProofUploadService>>();
    }

    [Fact]
    public async Task UploadProofAsync_WhenMimeTypeInvalid_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.UploadProofAsync(1, fileBytes, "application/pdf");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Apenas imagens JPG, PNG ou WebP são aceitas", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_WhenFileEmpty_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));

        // Act
        var result = await service.UploadProofAsync(1, Array.Empty<byte>(), "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Arquivo vazio", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_WhenFileTooLarge_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
        var largeFile = new byte[6 * 1024 * 1024]; // 6 MB

        // Act
        var result = await service.UploadProofAsync(1, largeFile, "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Arquivo muito grande", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_WhenConfirmationNotFound_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.UploadProofAsync(999, fileBytes, "image/jpeg");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Confirmação de presença não encontrada", result.Message);
    }

    [Fact]
    public async Task UploadProofAsync_WhenValid_SavesProofAndReturnsSuccess()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        var eventEntity = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(eventEntity);
        
        var confirmation = TestDataFactory.CreateEventConfirmation(eventEntity, user);
        db.EventConfirmations.Add(confirmation);
        
        await db.SaveChangesAsync();

        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.UploadProofAsync(confirmation.Id, fileBytes, "image/jpeg");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Comprovante enviado com sucesso", result.Message);
        Assert.NotNull(result.UploadedAt);
        
        // Verify saved to database (check the entity directly since it's tracked)
        Assert.Equal(fileBytes, confirmation.PixProofImageData);
        Assert.Equal("image/jpeg", confirmation.PixProofContentType);
        Assert.NotNull(confirmation.PixProofUploadedAt);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/JPEG")]
    [InlineData("IMAGE/PNG")]
    public async Task UploadProofAsync_AcceptsAllowedMimeTypes(string mimeType)
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        var eventEntity = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(eventEntity);
        
        var confirmation = TestDataFactory.CreateEventConfirmation(eventEntity, user);
        db.EventConfirmations.Add(confirmation);
        
        await db.SaveChangesAsync();

        var dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        
        var service = new PixProofUploadService(dbFactoryMock.Object, _loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.UploadProofAsync(confirmation.Id, fileBytes, mimeType);

        // Assert
        Assert.True(result.Success);
    }
}
