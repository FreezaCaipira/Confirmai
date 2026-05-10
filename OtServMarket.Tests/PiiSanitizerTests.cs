using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Services;

namespace Confirmai.Tests;

public class PiiSanitizerTests
{
    // ─── MaskEmail (single value) ─────────────────────────────────────────────

    [Theory]
    [InlineData("john@example.com",          "j***@example.com")]
    [InlineData("admin@otserv.market",        "a***@otserv.market")]
    [InlineData("j@domain.io",               "j***@domain.io")]
    [InlineData("first.last@sub.domain.com", "f***@sub.domain.com")]
    public void MaskEmail_ReturnsFirstCharPlusMaskedLocalPart(string input, string expected)
    {
        var result = PiiSanitizer.MaskEmail(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    public void MaskEmail_ReturnsUnchanged_WhenInputIsNotEmail(string input)
    {
        var result = PiiSanitizer.MaskEmail(input);
        Assert.Equal(input, result);
    }

    // ─── MaskEmailsInJson ─────────────────────────────────────────────────────

    [Fact]
    public void MaskEmailsInJson_ReturnsNull_WhenInputIsNull()
    {
        var result = PiiSanitizer.MaskEmailsInJson(null);
        Assert.Null(result);
    }

    [Fact]
    public void MaskEmailsInJson_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = PiiSanitizer.MaskEmailsInJson("");
        Assert.Equal("", result);
    }

    [Fact]
    public void MaskEmailsInJson_MasksEmailFieldValue()
    {
        var json = """{"email":"john.doe@example.com","action":"login"}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.NotNull(result);
        Assert.DoesNotContain("john.doe@example.com", result, StringComparison.Ordinal);
        Assert.Contains("j***@example.com", result, StringComparison.Ordinal);
        // Non-PII field must remain unchanged
        Assert.Contains("\"action\":\"login\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskEmailsInJson_MasksEmailFieldCaseInsensitive()
    {
        var json = """{"Email":"Admin@OtServ.Market","other":"value"}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.NotNull(result);
        Assert.DoesNotContain("Admin@OtServ.Market", result, StringComparison.Ordinal);
        Assert.Contains("A***@OtServ.Market", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskEmailsInJson_MasksBareEmailValue()
    {
        // An email in a field not named "email"
        var json = """{"actor":"seller@tibia.world","amount":10}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.NotNull(result);
        Assert.DoesNotContain("seller@tibia.world", result, StringComparison.Ordinal);
        Assert.Contains("s***@tibia.world", result, StringComparison.Ordinal);
        Assert.Contains("\"amount\":10", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskEmailsInJson_MasksMultipleEmails()
    {
        var json = """{"from":"alice@a.com","to":"bob@b.com","body":"hello"}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.NotNull(result);
        Assert.Contains("a***@a.com", result, StringComparison.Ordinal);
        Assert.Contains("b***@b.com", result, StringComparison.Ordinal);
        Assert.DoesNotContain("alice@a.com", result, StringComparison.Ordinal);
        Assert.DoesNotContain("bob@b.com", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskEmailsInJson_LeavesNonEmailJsonUnchanged()
    {
        var json = """{"orderId":42,"amount":0.05,"note":"noemail here"}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.Equal(json, result);
    }

    [Fact]
    public void MaskEmailsInJson_LeavesNumericAndBoolFieldsUnchanged()
    {
        var json = """{"active":true,"count":7,"ratio":0.5}""";

        var result = PiiSanitizer.MaskEmailsInJson(json);

        Assert.Equal(json, result);
    }

    // ─── Integration: LogService masks emails in AuditAsync metadata ──────────

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pii-test-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task LogService_AuditAsync_MasksEmailInMetadataJson()
    {
        await using var db = CreateDbContext();
        var service = new LogService(
            db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LogService>.Instance);

        await service.AuditAsync(
            eventType: AuditEvents.UserRegistered,
            entityType: AuditEntities.User,
            entityId: "u-1",
            message: "User registered",
            metadata: new { email = "newuser@example.com", username = "newuser" });

        var saved = Assert.Single(db.Logs);
        Assert.NotNull(saved.MetadataJson);
        Assert.DoesNotContain("newuser@example.com", saved.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("n***@example.com", saved.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("\"username\":\"newuser\"", saved.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogService_AuditAsync_MetadataWithoutEmail_IsUnchanged()
    {
        await using var db = CreateDbContext();
        var service = new LogService(
            db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LogService>.Instance);

        await service.AuditAsync(
            eventType: AuditEvents.OrderCreated,
            entityType: AuditEntities.Order,
            entityId: "42",
            message: "Order created",
            metadata: new { OrderId = 42, Amount = 0.05m });

        var saved = Assert.Single(db.Logs);
        Assert.NotNull(saved.MetadataJson);
        Assert.Contains("\"OrderId\":42", saved.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("\"Amount\":0.05", saved.MetadataJson, StringComparison.Ordinal);
    }
}
