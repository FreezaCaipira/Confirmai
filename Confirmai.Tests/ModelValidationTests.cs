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
        // SenderUserId is nullable (system/anonymous messages are valid)
        // Validation error is NOT expected on empty SenderUserId
        var msg = new UserMailboxMessage
        {
            SenderUserId = string.Empty,
            RecipientUserId = "recipient-1",
            Body = "Hello!"
        };

        Assert.False(HasError(msg, nameof(UserMailboxMessage.SenderUserId)));
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

    // ─── Venue ────────────────────────────────────────────────────────────────

    private static Venue ValidVenue() => new()
    {
        Name      = "Arena do Zé",
        Address   = "Rua das Flores, 123",
        City      = "Pouso Alegre",
        StateCode = "MG",
    };

    [Fact]
    public void Venue_Valid_WhenAllRequiredFieldsPresent()
    {
        Assert.True(IsValid(ValidVenue()));
    }

    [Fact]
    public void Venue_Invalid_WhenNameIsEmpty()
    {
        var v = ValidVenue();
        v.Name = string.Empty;
        Assert.True(HasError(v, nameof(Venue.Name)));
    }

    [Fact]
    public void Venue_Invalid_WhenNameExceeds120Chars()
    {
        var v = ValidVenue();
        v.Name = new string('a', 121);
        Assert.True(HasError(v, nameof(Venue.Name)));
    }

    [Fact]
    public void Venue_Invalid_WhenAddressIsEmpty()
    {
        var v = ValidVenue();
        v.Address = string.Empty;
        Assert.True(HasError(v, nameof(Venue.Address)));
    }

    [Fact]
    public void Venue_Invalid_WhenCityIsEmpty()
    {
        var v = ValidVenue();
        v.City = string.Empty;
        Assert.True(HasError(v, nameof(Venue.City)));
    }

    [Fact]
    public void Venue_Invalid_WhenStateCodeExceedsTwoChars()
    {
        var v = ValidVenue();
        v.StateCode = "MGX";
        Assert.True(HasError(v, nameof(Venue.StateCode)));
    }

    [Fact]
    public void Venue_Valid_WhenStateCodeIsExactlyTwoChars()
    {
        var v = ValidVenue();
        v.StateCode = "SP";
        Assert.True(IsValid(v));
    }

    // ─── Event ────────────────────────────────────────────────────────────────

    [Fact]
    public void Event_Invalid_WhenLocationIsEmpty()
    {
        var ev = new Event { Location = string.Empty, MaxPlayers = 10, GroupId = 1 };
        Assert.True(HasError(ev, nameof(Event.Location)));
    }

    [Fact]
    public void Event_Invalid_WhenLocationExceeds200Chars()
    {
        var ev = new Event { Location = new string('x', 201), MaxPlayers = 10, GroupId = 1 };
        Assert.True(HasError(ev, nameof(Event.Location)));
    }

    [Fact]
    public void Event_Valid_WhenLocationIsWithinLimit()
    {
        var ev = new Event { Location = "Rua A, 10 — Bairro", MaxPlayers = 10, GroupId = 1 };
        Assert.False(HasError(ev, nameof(Event.Location)));
    }

    // ─── EventConfirmation (lineup fields) ────────────────────────────────────

    private static EventConfirmation ValidConfirmation() => new()
    {
        EventId = 1,
        UserId  = "user-1",
    };

    [Fact]
    public void EventConfirmation_Valid_WhenTeamIdIsNull()
    {
        var c = ValidConfirmation();
        c.TeamId = null;
        Assert.True(IsValid(c));
    }

    [Fact]
    public void EventConfirmation_Valid_WhenTeamIdIsZero_TeamA()
    {
        var c = ValidConfirmation();
        c.TeamId = 0;
        Assert.True(IsValid(c));
    }

    [Fact]
    public void EventConfirmation_Valid_WhenTeamIdIsOne_TeamB()
    {
        var c = ValidConfirmation();
        c.TeamId = 1;
        Assert.True(IsValid(c));
    }

    [Fact]
    public void Event_Valid_WhenLineupConfirmedAtIsNull()
    {
        var ev = new Event { Location = "Quadra", MaxPlayers = 10, GroupId = 1, LineupConfirmedAt = null };
        Assert.False(HasError(ev, nameof(Event.LineupConfirmedAt)));
    }

    [Fact]
    public void Event_Valid_WhenLineupConfirmedAtIsSet()
    {
        var ev = new Event { Location = "Quadra", MaxPlayers = 10, GroupId = 1, LineupConfirmedAt = DateTime.UtcNow };
        Assert.False(HasError(ev, nameof(Event.LineupConfirmedAt)));
    }
}
