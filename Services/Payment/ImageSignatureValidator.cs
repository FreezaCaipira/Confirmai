namespace Confirmai.Services.Payment;

/// <summary>
/// Validates that uploaded bytes actually match the declared image type by
/// checking file signatures (magic bytes). The browser-supplied MIME type is
/// client-controlled and cannot be trusted on its own.
/// </summary>
public static class ImageSignatureValidator
{
    public static bool MatchesDeclaredType(byte[] bytes, string mimeType)
    {
        if (bytes.Length < 4) return false;

        return mimeType.ToLowerInvariant() switch
        {
            // JPEG: FF D8 FF
            "image/jpeg" => bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            // PNG: 89 50 4E 47 (the remaining 4 bytes of the signature are
            // always CR LF 1A LF; the first four already identify the format)
            "image/png" => bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
            // WebP: "RIFF" <4-byte size> "WEBP"
            "image/webp" => bytes.Length >= 12 &&
                bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
                bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P',
            _ => false
        };
    }
}
