using System.Text;
using Confirmai.Areas.Identity.Pages.Account;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class ResetPasswordModelTests
{
    private const string UserId = "user-1";
    private const string Email = "victim@example.com";
    private static readonly string EncodedCode =
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("raw-token"));

    private static ResetPasswordModel CreateModel(bool tokenValid)
    {
        var userManager = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<ApplicationUser>>().Object,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        userManager.Setup(m => m.FindByIdAsync(UserId))
            .ReturnsAsync(new ApplicationUser { Id = UserId, Email = Email });
        userManager.Setup(m => m.VerifyUserTokenAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>(), "raw-token"))
            .ReturnsAsync(tokenValid);

        return new ResetPasswordModel(userManager.Object, new UiTextService(new LanguagePreferenceService()));
    }

    [Fact]
    public async Task OnGet_WithValidTokenForUser_PrefillsEmailReadOnly()
    {
        var model = CreateModel(tokenValid: true);

        var result = await model.OnGetAsync(UserId, EncodedCode);

        Assert.IsType<PageResult>(result);
        Assert.Equal(Email, model.Input.Email);
        Assert.True(model.Input.EmailReadOnly);
        Assert.Equal("raw-token", model.Input.Code);
    }

    [Fact]
    public async Task OnGet_WithInvalidToken_DoesNotRevealEmail()
    {
        var model = CreateModel(tokenValid: false);

        var result = await model.OnGetAsync(UserId, EncodedCode);

        Assert.IsType<PageResult>(result);
        Assert.Equal(string.Empty, model.Input.Email);
        Assert.False(model.Input.EmailReadOnly);
    }

    [Fact]
    public async Task OnGet_WithoutUserId_KeepsEmailEditable()
    {
        var model = CreateModel(tokenValid: true);

        await model.OnGetAsync(null, EncodedCode);

        Assert.Equal(string.Empty, model.Input.Email);
        Assert.False(model.Input.EmailReadOnly);
    }

    [Fact]
    public async Task OnGet_WithoutCode_ReturnsBadRequest()
    {
        var model = CreateModel(tokenValid: true);

        var result = await model.OnGetAsync(UserId, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
