using System.ComponentModel.DataAnnotations;
using Confirmai.Models;

namespace Confirmai.Tests;

/// <summary>
/// Verifies DataAnnotation constraints declared on domain models using the standard
/// Validator API — the same path EF Core and ASP.NET model binding use.
/// </summary>
public class ModelValidationTests
{
    // ─── helpers ─────────────────────────────────────────────────────────────

    private static IList<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    private static bool IsValid(object model) => !Validate(model).Any();

    private static bool HasError(object model, string memberName) =>
        Validate(model).Any(r => r.MemberNames.Contains(memberName));

    // ─── UserMailboxMessage ───────────────────────────────────────────────────

    [Fact]
    public void UserMailboxMessage_Valid_WhenRequiredFieldsPresent()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = "recipient-1",
            Body = "Hello!"
        };

        Assert.True(IsValid(msg));
    }

    [Fact]
    public void UserMailboxMessage_Invalid_WhenSenderUserIdIsEmpty()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = string.Empty,
            RecipientUserId = "recipient-1",
            Body = "Hello!"
        };

        Assert.True(HasError(msg, nameof(UserMailboxMessage.SenderUserId)));
    }

    [Fact]
    public void UserMailboxMessage_Invalid_WhenRecipientUserIdIsEmpty()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = string.Empty,
            Body = "Hello!"
        };

        Assert.True(HasError(msg, nameof(UserMailboxMessage.RecipientUserId)));
    }

    [Fact]
    public void UserMailboxMessage_Invalid_WhenBodyIsEmpty()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = "recipient-1",
            Body = string.Empty
        };

        Assert.True(HasError(msg, nameof(UserMailboxMessage.Body)));
    }

    [Fact]
    public void UserMailboxMessage_Invalid_WhenBodyExceeds4000Chars()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = "recipient-1",
            Body = new string('x', 4001)
        };

        Assert.True(HasError(msg, nameof(UserMailboxMessage.Body)));
    }

    [Fact]
    public void UserMailboxMessage_Valid_WhenBodyIsExactly4000Chars()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = "recipient-1",
            Body = new string('x', 4000)
        };

        Assert.True(IsValid(msg));
    }

    [Fact]
    public void UserMailboxMessage_Invalid_WhenSubjectExceeds180Chars()
    {
        var msg = new UserMailboxMessage
        {
            SenderUserId = "sender-1",
            RecipientUserId = "recipient-1",
            Body = "Hello!",
            Subject = new string('s', 181)
        };

        Assert.True(HasError(msg, nameof(UserMailboxMessage.Subject)));
    }
}
