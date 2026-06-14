using System.Text;

namespace Confirmai.Services.Payment.Shared;

/// <summary>
/// Shared utility for reading and size-validating webhook request bodies.
/// Eliminates duplicated body-reading logic across webhook services.
/// </summary>
public static class WebhookBodyReader
{
    public const int DefaultMaxBodyBytes = 64 * 1024;

    /// <summary>
    /// Reads the request body with size validation.
    /// Returns <c>null</c> if the body exceeds the max size.
    /// </summary>
    public static async Task<string?> ReadBodyAsync(HttpContext context, int maxBytes = DefaultMaxBodyBytes)
    {
        if (context.Request.ContentLength > maxBytes)
            return null;

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        if (Encoding.UTF8.GetByteCount(body) > maxBytes)
            return null;

        return body;
    }
}
