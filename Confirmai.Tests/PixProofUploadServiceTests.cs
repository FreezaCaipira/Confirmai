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

    private static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0 };
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] WebpBytes = { 0x52, 0x49, 0x46, 0x46, 0x24, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };

    public PixProofUploadServiceTests()
    {
        _loggerMock = new Mock<ILogger<PixProofUploadService>>();
    }

    private static PixProofUploadService BuildService(AppDbContext db, Mock<ILogger<PixProofUploadService>> loggerMock, out Mock<IDbContextFactory<AppDbContext>> dbFactoryMock)
    {
        dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        dbFactoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        return new PixProofUploadService(dbFactoryMock.Object, loggerMock.Object, new LogService(dbFactoryMock.Object, NullLogger<LogService>.Instance));
    }

    [Fact]
    public async Task UploadProofAsync_WhenMimeTypeInvalid_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var service = BuildService(db, _loggerMock, out _);

        // Act
        var result = await service.UploadProofAsync(1, JpegBytes, "application/pdf", "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.InvalidImage, result.Error);
    }

    [Fact]
    public async Task UploadProofAsync_WhenSignatureMismatch_ReturnsInvalidImage()
    {
        // Arrange — declared as JPEG but the bytes are not a JPEG
        var db = TestDataFactory.CreateDbContext();
        var service = BuildService(db, _loggerMock, out _);
        var fileBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00 }; // EXE magic

        // Act
        var result = await service.UploadProofAsync(1, fileBytes, "image/jpeg", "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.InvalidImage, result.Error);
    }

    [Fact]
    public async Task UploadProofAsync_WhenFileEmpty_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var service = BuildService(db, _loggerMock, out _);

        // Act
        var result = await service.UploadProofAsync(1, Array.Empty<byte>(), "image/jpeg", "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.InvalidImage, result.Error);
    }

    [Fact]
    public async Task UploadProofAsync_WhenFileTooLarge_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var service = BuildService(db, _loggerMock, out _);
        var largeFile = new byte[6 * 1024 * 1024]; // 6 MB

        // Act
        var result = await service.UploadProofAsync(1, largeFile, "image/jpeg", "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.FileTooLarge, result.Error);
    }

    [Fact]
    public async Task UploadProofAsync_WhenConfirmationNotFound_ReturnsError()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var service = BuildService(db, _loggerMock, out _);

        // Act
        var result = await service.UploadProofAsync(999, JpegBytes, "image/jpeg", "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.NotFound, result.Error);
    }

    [Fact]
    public async Task UploadProofAsync_WhenCallerIsNotOwner_ReturnsForbidden()
    {
        // Arrange — confirmation belongs to "user-1", caller is "intruder"
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        var eventEntity = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(eventEntity);
        var confirmation = TestDataFactory.CreateEventConfirmation(eventEntity, user);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var service = BuildService(db, _loggerMock, out _);

        // Act
        var result = await service.UploadProofAsync(confirmation.Id, JpegBytes, "image/jpeg", "intruder");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.Forbidden, result.Error);
        Assert.Null(confirmation.PixProofImageData);
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

        var service = BuildService(db, _loggerMock, out _);

        // Act
        var result = await service.UploadProofAsync(confirmation.Id, JpegBytes, "image/jpeg", "user-1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(PixProofUploadError.None, result.Error);
        Assert.NotNull(result.UploadedAt);

        // Verify saved to database (check the entity directly since it's tracked)
        Assert.Equal(JpegBytes, confirmation.PixProofImageData);
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

        var service = BuildService(db, _loggerMock, out _);
        var fileBytes = mimeType.EndsWith("png", StringComparison.OrdinalIgnoreCase) ? PngBytes
            : mimeType.EndsWith("webp", StringComparison.OrdinalIgnoreCase) ? WebpBytes
            : JpegBytes;

        // Act
        var result = await service.UploadProofAsync(confirmation.Id, fileBytes, mimeType, "user-1");

        // Assert
        Assert.True(result.Success);
    }
}
