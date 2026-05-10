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

    // ─── TibiaServer ─────────────────────────────────────────────────────────

    [Fact]
    public void TibiaServer_Valid_WhenRequiredFieldsPresent()
    {
        var server = new TibiaServer { Name = "Test Server" };

        Assert.True(IsValid(server));
    }

    [Fact]
    public void TibiaServer_Invalid_WhenNameIsEmpty()
    {
        var server = new TibiaServer { Name = string.Empty };

        Assert.True(HasError(server, nameof(TibiaServer.Name)));
    }

    [Fact]
    public void TibiaServer_Invalid_WhenNameExceeds120Chars()
    {
        var server = new TibiaServer { Name = new string('a', 121) };

        Assert.True(HasError(server, nameof(TibiaServer.Name)));
    }

    [Fact]
    public void TibiaServer_Valid_WhenNameIsExactly120Chars()
    {
        var server = new TibiaServer { Name = new string('a', 120) };

        Assert.True(IsValid(server));
    }

    [Fact]
    public void TibiaServer_Invalid_WhenRegionExceeds60Chars()
    {
        var server = new TibiaServer { Name = "Valid", Region = new string('r', 61) };

        Assert.True(HasError(server, nameof(TibiaServer.Region)));
    }

    [Fact]
    public void TibiaServer_Invalid_WhenWebsiteUrlExceeds200Chars()
    {
        var server = new TibiaServer { Name = "Valid", WebsiteUrl = new string('u', 201) };

        Assert.True(HasError(server, nameof(TibiaServer.WebsiteUrl)));
    }

    [Fact]
    public void TibiaServer_Invalid_WhenTibiaVersionExceeds40Chars()
    {
        var server = new TibiaServer { Name = "Valid", TibiaVersion = new string('v', 41) };

        Assert.True(HasError(server, nameof(TibiaServer.TibiaVersion)));
    }

    // ─── ServerRegistrationRequest ────────────────────────────────────────────

    [Fact]
    public void ServerRegistrationRequest_Valid_WhenRequiredFieldsPresent()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = "My Server",
            TibiaVersion = "12.65"
        };

        Assert.True(IsValid(req));
    }

    [Fact]
    public void ServerRegistrationRequest_Invalid_WhenRequestedNameIsEmpty()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = string.Empty,
            TibiaVersion = "12.65"
        };

        Assert.True(HasError(req, nameof(ServerRegistrationRequest.RequestedName)));
    }

    [Fact]
    public void ServerRegistrationRequest_Invalid_WhenRequestedNameExceeds120Chars()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = new string('a', 121),
            TibiaVersion = "12.65"
        };

        Assert.True(HasError(req, nameof(ServerRegistrationRequest.RequestedName)));
    }

    [Fact]
    public void ServerRegistrationRequest_Invalid_WhenTibiaVersionIsEmpty()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = "My Server",
            TibiaVersion = string.Empty
        };

        Assert.True(HasError(req, nameof(ServerRegistrationRequest.TibiaVersion)));
    }

    [Fact]
    public void ServerRegistrationRequest_Invalid_WhenTibiaVersionExceeds40Chars()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = "My Server",
            TibiaVersion = new string('v', 41)
        };

        Assert.True(HasError(req, nameof(ServerRegistrationRequest.TibiaVersion)));
    }

    [Fact]
    public void ServerRegistrationRequest_Invalid_WhenWebsiteUrlExceeds200Chars()
    {
        var req = new ServerRegistrationRequest
        {
            RequestedName = "My Server",
            TibiaVersion = "12.65",
            WebsiteUrl = new string('u', 201)
        };

        Assert.True(HasError(req, nameof(ServerRegistrationRequest.WebsiteUrl)));
    }

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
