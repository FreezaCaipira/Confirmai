using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Confirmai.Pages.Payment
{
    /// <summary>
    /// Builds EMVCo-compliant PIX static payload (QR code data).
    /// Pure utility logic for PIX payment encoding and validation.
    /// </summary>
    public static class PixPayloadBuilder
    {
        /// <summary>
        /// Constructs an EMVCo PIX static payload with all required fields.
        /// </summary>
        public static string BuildPixPayload(string pixKey, decimal amount, string merchantName, string merchantCity, string txId)
        {
            var merchantAccountInfo =
                Tlv("00", "br.gov.bcb.pix") +
                Tlv("01", pixKey);

            var additionalDataField = Tlv("05", txId);

            var payload =
                Tlv("00", "01") +
                Tlv("01", "12") +
                Tlv("26", merchantAccountInfo) +
                Tlv("52", "0000") +
                Tlv("53", "986") +
                (amount > 0m ? Tlv("54", amount.ToString("0.00", CultureInfo.InvariantCulture)) : string.Empty) +
                Tlv("58", "BR") +
                Tlv("59", merchantName) +
                Tlv("60", merchantCity) +
                Tlv("62", additionalDataField) +
                "6304";

            var crc = ComputeCrc16(payload);
            return payload + crc;
        }

        /// <summary>
        /// Builds a transaction ID from product ID and timestamp (max 25 chars).
        /// </summary>
        public static string BuildPixTxId(int productId)
        {
            var raw = $"OTS{productId}{DateTime.UtcNow:ddHHmmss}";
            return raw.Length > 25 ? raw[..25] : raw;
        }

        /// <summary>
        /// Encodes a TLV (Tag-Length-Value) field.
        /// </summary>
        private static string Tlv(string id, string value)
        {
            return $"{id}{value.Length:D2}{value}";
        }

        /// <summary>
        /// Computes CRC-16 checksum for PIX payload validation.
        /// </summary>
        private static string ComputeCrc16(string payload)
        {
            ushort crc = 0xFFFF;
            var bytes = Encoding.ASCII.GetBytes(payload);

            foreach (var b in bytes)
            {
                crc ^= (ushort)(b << 8);

                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 0x8000) != 0
                        ? (ushort)((crc << 1) ^ 0x1021)
                        : (ushort)(crc << 1);
                }
            }

            return crc.ToString("X4");
        }

        /// <summary>
        /// Sanitizes PIX merchant name/city fields: removes diacritics, converts to uppercase ASCII.
        /// </summary>
        public static string SanitizePixText(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var normalized = input.Normalize(NormalizationForm.FormD);
            var noDiacritics = new string(normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            var asciiUpper = noDiacritics.ToUpperInvariant();
            var allowed = new string(asciiUpper
                .Where(c => char.IsLetterOrDigit(c) || c == ' ')
                .ToArray());

            var compact = string.Join(" ", allowed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (compact.Length <= maxLength)
            {
                return compact;
            }

            return compact[..maxLength];
        }
    }
}
