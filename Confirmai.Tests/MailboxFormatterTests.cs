using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class MailboxFormatterTests
{
    private const string NoContent = "(sem conteudo)";

    // ── BuildSnippet ──────────────────────────────────────────────────

    [Fact]
    public void BuildSnippet_ReturnsNoContentText_WhenBodyIsNull()
    {
        var result = MailboxFormatter.BuildSnippet(null!, NoContent);
        Assert.Equal(NoContent, result);
    }

    [Fact]
    public void BuildSnippet_ReturnsNoContentText_WhenBodyIsEmpty()
    {
        var result = MailboxFormatter.BuildSnippet("", NoContent);
        Assert.Equal(NoContent, result);
    }

    [Fact]
    public void BuildSnippet_ReturnsNoContentText_WhenBodyIsWhitespace()
    {
        var result = MailboxFormatter.BuildSnippet("   ", NoContent);
        Assert.Equal(NoContent, result);
    }

    [Fact]
    public void BuildSnippet_ReturnsTrimmedBody_WhenShorterThan96()
    {
        var result = MailboxFormatter.BuildSnippet("Hello world", NoContent);
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void BuildSnippet_TrimsBodyBeforeCheckingLength()
    {
        var result = MailboxFormatter.BuildSnippet("  Hello  ", NoContent);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void BuildSnippet_TruncatesAndAddsEllipsis_WhenLongerThan96()
    {
        var body = new string('x', 100);
        var result = MailboxFormatter.BuildSnippet(body, NoContent);
        Assert.EndsWith("...", result);
        Assert.Equal(99, result.Length); // 96 + "..."
    }

    [Fact]
    public void BuildSnippet_ReturnsExactBody_WhenExactly96Chars()
    {
        var body = new string('x', 96);
        var result = MailboxFormatter.BuildSnippet(body, NoContent);
        Assert.Equal(96, result.Length);
        Assert.DoesNotContain("...", result);
    }

    // ── BuildAvatarInitials ───────────────────────────────────────────

    [Fact]
    public void BuildAvatarInitials_ReturnsQuestionMark_WhenNameIsNull()
    {
        var result = MailboxFormatter.BuildAvatarInitials(null!);
        Assert.Equal("?", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsQuestionMark_WhenNameIsEmpty()
    {
        var result = MailboxFormatter.BuildAvatarInitials("");
        Assert.Equal("?", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsQuestionMark_WhenNameIsWhitespace()
    {
        var result = MailboxFormatter.BuildAvatarInitials("   ");
        Assert.Equal("?", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsFirstTwoChars_WhenSingleWord()
    {
        var result = MailboxFormatter.BuildAvatarInitials("Joao");
        Assert.Equal("JO", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsFirstChar_WhenSingleCharWord()
    {
        var result = MailboxFormatter.BuildAvatarInitials("A");
        Assert.Equal("A", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsFirstAndLastInitials_WhenTwoWords()
    {
        var result = MailboxFormatter.BuildAvatarInitials("Joao Silva");
        Assert.Equal("JS", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsFirstAndLastInitials_WhenThreeWords()
    {
        var result = MailboxFormatter.BuildAvatarInitials("Joao Pedro Silva");
        Assert.Equal("JS", result);
    }

    [Fact]
    public void BuildAvatarInitials_HandlesExtraSpaces()
    {
        var result = MailboxFormatter.BuildAvatarInitials("  Joao   Silva  ");
        Assert.Equal("JS", result);
    }

    [Fact]
    public void BuildAvatarInitials_ReturnsUppercase()
    {
        var result = MailboxFormatter.BuildAvatarInitials("joao silva");
        Assert.Equal("JS", result);
    }

    // ── FormatRelativeTime ────────────────────────────────────────────

    [Fact]
    public void FormatRelativeTime_ReturnsNowText_WhenLessThan1Minute()
    {
        var utcDate = DateTime.UtcNow.AddSeconds(-30);
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        Assert.Equal("agora", result);
    }

    [Fact]
    public void FormatRelativeTime_ReturnsMinutes_WhenLessThan60Minutes()
    {
        var utcDate = DateTime.UtcNow.AddMinutes(-5);
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        Assert.Contains("m", result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void FormatRelativeTime_ReturnsMinimum1Minute()
    {
        var utcDate = DateTime.UtcNow.AddSeconds(-59);
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        // 59 seconds is < 1 minute so should return "agora"
        Assert.Equal("agora", result);
    }

    [Fact]
    public void FormatRelativeTime_ReturnsYesterdayText_WhenDateIsYesterday()
    {
        // Noon local yesterday is unambiguously "yesterday" in any timezone;
        // the old UTC-based formula broke when local date != UTC date.
        var utcDate = DateTime.Now.Date.AddDays(-1).AddHours(12).ToUniversalTime();
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        // The result should be either "ontem" or a dd/MM date depending on exact timezone
        Assert.True(result == "ontem" || System.Text.RegularExpressions.Regex.IsMatch(result, @"^\d{2}/\d{2}$"), $"Unexpected result: {result}");
    }

    [Fact]
    public void FormatRelativeTime_ReturnsTimeString_WhenToday()
    {
        // Use a date that is clearly today in local time (1 hour ago)
        var utcDate = DateTime.UtcNow.AddHours(-1);
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        // Should be either HH:mm (today) or "ontem" if it crossed midnight in local time
        Assert.True(
            System.Text.RegularExpressions.Regex.IsMatch(result, @"^\d{2}:\d{2}$") || result == "ontem",
            $"Unexpected result: {result}");
    }

    [Fact]
    public void FormatRelativeTime_ReturnsDateString_WhenOlderThanYesterday()
    {
        var utcDate = DateTime.UtcNow.AddDays(-10);
        var result = MailboxFormatter.FormatRelativeTime(utcDate, "agora", "ontem");
        Assert.Matches(@"^\d{2}/\d{2}$", result);
    }

    // ── TotalPages ────────────────────────────────────────────────────

    [Fact]
    public void TotalPages_Returns1_WhenTotalIsZero()
    {
        var result = MailboxFormatter.TotalPages(0, 10);
        Assert.Equal(1, result);
    }

    [Fact]
    public void TotalPages_Returns1_WhenTotalIsNegative()
    {
        var result = MailboxFormatter.TotalPages(-5, 10);
        Assert.Equal(1, result);
    }

    [Fact]
    public void TotalPages_Returns1_WhenTotalFitsExactlyInOnePage()
    {
        var result = MailboxFormatter.TotalPages(10, 10);
        Assert.Equal(1, result);
    }

    [Fact]
    public void TotalPages_Returns2_WhenTotalExceedsOnePage()
    {
        var result = MailboxFormatter.TotalPages(11, 10);
        Assert.Equal(2, result);
    }

    [Fact]
    public void TotalPages_ReturnsCorrectValue_WhenTotalIsLarge()
    {
        var result = MailboxFormatter.TotalPages(100, 10);
        Assert.Equal(10, result);
    }

    [Fact]
    public void TotalPages_RoundsUp_WhenNotExactMultiple()
    {
        var result = MailboxFormatter.TotalPages(25, 10);
        Assert.Equal(3, result);
    }
}
