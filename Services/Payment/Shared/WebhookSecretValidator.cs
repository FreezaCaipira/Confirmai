using System.Security.Cryptography;
using System.Text;

namespace Confirmai.Services.Payment.Shared;

/// <summary>
/// Timing-safe webhook secret validation shared across payment webhook services.
/// </summary>
public static class WebhookSecretValidator
{
    /// <summary>
    /// Validates a query-string webhook secret using constant-time comparison.
    /// Returns <c>true</c> if valid (or if no secret is configured).
    /// </summary>
    public static bool Validate(HttpContext context, string? expectedSecret)
    {
        if (string.IsNullOrWhiteSpace(expectedSecret))
            return true;

        var provided = context.Request.Query["webhookSecret"].ToString() ?? "";
        return FixedTimeCompare(provided, expectedSecret);
    }

    /// <summary>
    /// Performs a constant-time comparison of two strings to prevent timing attacks.
    /// </summary>
    public static bool FixedTimeCompare(string provided, string expected)
    {
        if (provided.Length != expected.Length)
            return false;

        var providedBytes = Encoding.UTF8.GetBytes(provided.PadRight(expected.Length));
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
