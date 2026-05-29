using System.Text;
using Microsoft.AspNetCore.Http;

namespace Confirmai.Tests;

internal static class WebhookTestFactory
{
    public static DefaultHttpContext CreateContext(string secret, string invoiceId, string eventType,
        string? deliveryId = null, long? timestamp = null)
    {
        var parts = new List<string>
        {
            $"\"invoiceId\":\"{invoiceId}\"",
            $"\"type\":\"{eventType}\""
        };
        if (deliveryId != null) parts.Add($"\"deliveryId\":\"{deliveryId}\"");
        if (timestamp.HasValue) parts.Add($"\"timestamp\":{timestamp.Value}");

        var body = "{" + string.Join(",", parts) + "}";
        return CreateContextRaw(secret, body);
    }

    public static DefaultHttpContext CreateContextRaw(string secret, string rawBody)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-BTCPay-Secret"] = secret;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawBody));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Request.Body.Position = 0;
        return context;
    }

    /// <summary>
    /// Creates an <see cref="HttpContext"/> for EfiBank webhook tests.
    /// EfiBank uses a query-string secret (<c>?webhookSecret=...</c>) instead of a header.
    /// </summary>
    public static DefaultHttpContext CreateEfiBankContext(string rawBody, string? secret = null)
    {
        var context = new DefaultHttpContext();

        if (secret is not null)
            context.Request.QueryString = new QueryString($"?webhookSecret={Uri.EscapeDataString(secret)}");

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawBody));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Request.Body.Position = 0;
        return context;
    }
}

