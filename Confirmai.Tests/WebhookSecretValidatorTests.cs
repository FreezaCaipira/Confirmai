using Confirmai.Services.Payment.Shared;
using Microsoft.AspNetCore.Http;

namespace Confirmai.Tests;

public class WebhookSecretValidatorTests
{
    [Fact]
    public void Validate_WithNoExpectedSecret_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();

        // Act
        var result = WebhookSecretValidator.Validate(context, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithEmptyExpectedSecret_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();

        // Act
        var result = WebhookSecretValidator.Validate(context, "");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithWhitespaceExpectedSecret_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();

        // Act
        var result = WebhookSecretValidator.Validate(context, "   ");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithMatchingSecret_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "webhookSecret", "test-secret" }
        });

        // Act
        var result = WebhookSecretValidator.Validate(context, "test-secret");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithMismatchedSecret_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "webhookSecret", "wrong-secret" }
        });

        // Act
        var result = WebhookSecretValidator.Validate(context, "test-secret");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithMissingSecretInQuery_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();

        // Act
        var result = WebhookSecretValidator.Validate(context, "test-secret");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FixedTimeCompare_WithMatchingStrings_ReturnsTrue()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("test-secret", "test-secret");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void FixedTimeCompare_WithDifferentStrings_ReturnsFalse()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("test-secret", "wrong-secret");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FixedTimeCompare_WithDifferentLengths_ReturnsFalse()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("test", "test-secret");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FixedTimeCompare_WithEmptyStrings_ReturnsTrue()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("", "");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void FixedTimeCompare_WithOneEmptyString_ReturnsFalse()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("", "test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FixedTimeCompare_WithShorterProvidedString_PadsAndCompares()
    {
        // Act
        var result = WebhookSecretValidator.FixedTimeCompare("test", "test-secret");

        // Assert
        Assert.False(result); // Padded with spaces, won't match
    }
}
