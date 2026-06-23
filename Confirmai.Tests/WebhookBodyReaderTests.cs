using Confirmai.Services.Payment.Shared;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Confirmai.Tests;

public class WebhookBodyReaderTests
{
    [Fact]
    public async Task ReadBodyAsync_WithValidBody_ReturnsBody()
    {
        // Arrange
        var body = "test-body-content";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = body.Length;

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context);

        // Assert
        Assert.Equal(body, result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithBodyExceedingContentLength_ReturnsNull()
    {
        // Arrange
        var body = "test-body-content";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = 5; // Less than actual body

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context, maxBytes: 10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithBodyExceedingMaxBytes_ReturnsNull()
    {
        // Arrange
        var body = new string('a', 100);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = body.Length;

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context, maxBytes: 50);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithEmptyBody_ReturnsEmptyString()
    {
        // Arrange
        var body = "";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = 0;

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithDefaultMaxBytes_Uses64KB()
    {
        // Arrange
        var body = new string('a', 100);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = body.Length;

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context);

        // Assert
        Assert.Equal(body, result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithUnicodeBody_ReturnsCorrectBody()
    {
        // Arrange
        var body = "test-unicode-ñ-é-á";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = Encoding.UTF8.GetByteCount(body);

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context);

        // Assert
        Assert.Equal(body, result);
    }

    [Fact]
    public async Task ReadBodyAsync_WithUnicodeBodyExceedingMaxBytes_ReturnsNull()
    {
        // Arrange
        var body = new string('ñ', 100); // Multi-byte characters
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = Encoding.UTF8.GetByteCount(body);

        // Act
        var result = await WebhookBodyReader.ReadBodyAsync(context, maxBytes: 50);

        // Assert
        Assert.Null(result);
    }
}
