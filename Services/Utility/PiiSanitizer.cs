using Confirmai.Services.Utility;
using System.Text.RegularExpressions;

namespace Confirmai.Services.Utility;

/// <summary>
/// Utility for masking personally-identifiable information (PII) before it
/// is stored in audit log metadata, in compliance with LGPD / GDPR.
///
/// Rules applied:
///   - Email addresses in JSON strings are masked to preserve the first
///     character of the local part and the full domain:
///     "john.doe@example.com" ? "j***@example.com"
///
/// Usage: call <see cref="MaskEmailsInJson"/> on the serialized MetadataJson
/// string before persisting to the database.
/// </summary>
public static partial class PiiSanitizer
{
    // Matches a JSON string value that looks like an email address.
    // Group 1: the first character of the local part.
    // Group 2: rest of local part (everything before @, after first char).
    // Group 3: the domain part (everything from @ to end of email, before the closing quote).
    [GeneratedRegex(
        @"(?<=""[^""]*?email[^""]*?""\s*:\s*"")([A-Za-z0-9._%+\-])([A-Za-z0-9._%+\-]*)(@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})(?="")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex EmailInJsonValueByKeyRegex();

    // Matches bare email-like tokens that appear as JSON string values (not just for "email" keys).
    // This catches fields like "user", "actor", "from", etc. that happen to contain an email.
    [GeneratedRegex(
        @"(?<="")([A-Za-z0-9._%+\-])([A-Za-z0-9._%+\-]+)(@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})(?="")",
        RegexOptions.Compiled,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex BareEmailInJsonValueRegex();

    /// <summary>
    /// Returns <paramref name="json"/> with any email addresses masked.
    /// Safe to call with <c>null</c> � returns <c>null</c> unchanged.
    /// </summary>
    public static string? MaskEmailsInJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        try
        {
            // First pass: fields whose key contains "email"
            var result = EmailInJsonValueByKeyRegex().Replace(json, m =>
                m.Groups[1].Value + "***" + m.Groups[3].Value);

            // Second pass: any remaining bare email-looking values
            result = BareEmailInJsonValueRegex().Replace(result, m =>
                m.Groups[1].Value + "***" + m.Groups[3].Value);

            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            // If the regex times out on unusually large/pathological input,
            // return the original rather than crashing the audit write.
            return json;
        }
    }

    /// <summary>
    /// Masks an individual email address string.
    /// Returns the input unchanged if it does not look like an email.
    /// </summary>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return email;

        return email[0] + "***" + email[atIndex..];
    }
}
