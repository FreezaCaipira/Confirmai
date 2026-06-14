using Confirmai.Pages.Payment;

namespace Confirmai.Tests;

public class PixPayloadBuilderTests
{
    // ── BuildPixPayload ─────────────────────────────────────────────────────

    [Fact]
    public void BuildPixPayload_ReturnsNonEmptyString()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "12345678900", 10.50m, "LOJA TESTE", "SAO PAULO", "TX123");

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void BuildPixPayload_StartsWithExpectedTlvHeader()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "12345678900", 1m, "LOJA", "CIDADE", "TX1");

        // EMVCo payload format ID "00" with value "01"
        Assert.StartsWith("000201", result);
    }

    [Fact]
    public void BuildPixPayload_ContainsPixKey()
    {
        const string pixKey = "minha-chave-pix@email.com";
        var result = PixPayloadBuilder.BuildPixPayload(
            pixKey, 5m, "MERCHANT", "CITY", "TX99");

        Assert.Contains(pixKey, result);
    }

    [Fact]
    public void BuildPixPayload_ContainsMerchantNameAndCity()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 1m, "MINHA LOJA", "BRASILIA", "TX1");

        Assert.Contains("MINHA LOJA", result);
        Assert.Contains("BRASILIA", result);
    }

    [Fact]
    public void BuildPixPayload_ContainsTxId()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 1m, "M", "C", "TXID12345");

        Assert.Contains("TXID12345", result);
    }

    [Fact]
    public void BuildPixPayload_ContainsAmountFormatted()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 99.99m, "M", "C", "TX1");

        Assert.Contains("99.99", result);
    }

    [Fact]
    public void BuildPixPayload_OmitsAmountField_WhenAmountIsZero()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 0m, "M", "C", "TX1");

        // Tag "54" is the amount field – should not appear when amount is 0
        Assert.DoesNotContain("5404", result);
    }

    [Fact]
    public void BuildPixPayload_ContainsBrazilCountryCode()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 1m, "M", "C", "TX1");

        // Tag "58" value "BR"
        Assert.Contains("5802BR", result);
    }

    [Fact]
    public void BuildPixPayload_ContainsCurrencyCode986()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 1m, "M", "C", "TX1");

        // Tag "53" value "986" (BRL)
        Assert.Contains("5303986", result);
    }

    [Fact]
    public void BuildPixPayload_EndsWithCrc16Checksum()
    {
        var result = PixPayloadBuilder.BuildPixPayload(
            "key", 1m, "M", "C", "TX1");

        // Payload must contain "6304" marker followed by 4 hex chars
        var crcIdx = result.LastIndexOf("6304", StringComparison.Ordinal);
        Assert.True(crcIdx >= 0, "CRC marker '6304' not found");
        var crcValue = result[(crcIdx + 4)..];
        Assert.Equal(4, crcValue.Length);
        Assert.True(crcValue.All(c => "0123456789ABCDEF".Contains(c)),
            $"CRC value '{crcValue}' contains non-hex characters");
    }

    [Fact]
    public void BuildPixPayload_DeterministicForSameInput()
    {
        var a = PixPayloadBuilder.BuildPixPayload("key", 10m, "M", "C", "TX1");
        var b = PixPayloadBuilder.BuildPixPayload("key", 10m, "M", "C", "TX1");

        Assert.Equal(a, b);
    }

    // ── BuildPixTxId ────────────────────────────────────────────────────────

    [Fact]
    public void BuildPixTxId_StartsWithOTSPrefix()
    {
        var txId = PixPayloadBuilder.BuildPixTxId(42);

        Assert.StartsWith("OTS", txId);
    }

    [Fact]
    public void BuildPixTxId_ContainsProductId()
    {
        var txId = PixPayloadBuilder.BuildPixTxId(999);

        Assert.Contains("999", txId);
    }

    [Fact]
    public void BuildPixTxId_MaxLength25()
    {
        var txId = PixPayloadBuilder.BuildPixTxId(123456789);

        Assert.True(txId.Length <= 25, $"TxId '{txId}' exceeds 25 chars (length={txId.Length})");
    }

    [Fact]
    public void BuildPixTxId_SmallProductId_DoesNotExceed25()
    {
        var txId = PixPayloadBuilder.BuildPixTxId(1);

        Assert.True(txId.Length <= 25);
    }

    // ── SanitizePixText ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("João da Silva", 25, "JOAO DA SILVA")]
    [InlineData("café", 10, "CAFE")]
    [InlineData("André Müller", 25, "ANDRE MULLER")]
    public void SanitizePixText_RemovesDiacriticsAndUppercases(string input, int maxLen, string expected)
    {
        var result = PixPayloadBuilder.SanitizePixText(input, maxLen);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizePixText_ReturnsEmpty_ForBlankInput(string? input)
    {
        var result = PixPayloadBuilder.SanitizePixText(input!, 25);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizePixText_TruncatesAtMaxLength()
    {
        var result = PixPayloadBuilder.SanitizePixText("ABCDEFGHIJ", 5);

        Assert.Equal("ABCDE", result);
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void SanitizePixText_RemovesSpecialCharacters()
    {
        var result = PixPayloadBuilder.SanitizePixText("Test@#$%!", 25);

        Assert.Equal("TEST", result);
    }

    [Fact]
    public void SanitizePixText_CollapsesMultipleSpaces()
    {
        var result = PixPayloadBuilder.SanitizePixText("A   B   C", 25);

        Assert.Equal("A B C", result);
    }

    [Fact]
    public void SanitizePixText_RetainsDigits()
    {
        var result = PixPayloadBuilder.SanitizePixText("Loja 123", 25);

        Assert.Equal("LOJA 123", result);
    }
}
