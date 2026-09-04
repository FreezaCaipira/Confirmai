using System.Security.Claims;
using Confirmai.Areas.Identity.Pages.Account;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class ExternalLoginModelTests
{
    private static ExternalLoginModel CreateModel(
        out Mock<SignInManager<ApplicationUser>> signInManager,
        out Mock<UserManager<ApplicationUser>> userManager,
        out Mock<IDbContextFactory<AppDbContext>> dbFactory,
        string? email = "test@example.com",
        string providerKey = "google-user-id",
        string? emailVerified = null)
    {
        userManager = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<ApplicationUser>>().Object,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        signInManager = new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<ApplicationUser>>().Object);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactory.Setup(f => f.CreateDbContext()).Returns(() => new AppDbContext(options));

        var log = new LogService(dbFactory.Object, NullLogger<LogService>.Instance);
        var t = new UiTextService(new LanguagePreferenceService());
        var claimsExtractor = new ExternalLoginClaimsExtractor(
            new TestWebHostEnvironment(),
            new HttpClient(new StubHttpMessageHandler()));

        var model = new ExternalLoginModel(signInManager.Object, userManager.Object, log, t, claimsExtractor);

        var httpContext = new DefaultHttpContext();
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
        model.PageContext = pageContext;
        model.TempData = new Mock<ITempDataDictionary>().Object;

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(s => s);
        model.Url = urlHelper.Object;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email ?? "")
        };
        if (!string.IsNullOrWhiteSpace(providerKey))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, providerKey));
        }
        if (emailVerified is not null)
        {
            claims.Add(new Claim("email_verified", emailVerified));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
        var info = new ExternalLoginInfo(principal, "Google", providerKey, "Google");

        signInManager.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
        signInManager.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        return model;
    }

    [Fact]
    public async Task OnGetCallbackAsync_ExistingLogin_SucceedsAndRedirects()
    {
        var model = CreateModel(out var signInManager, out var userManager, out var dbFactory);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Success);
        userManager
            .Setup(x => x.FindByLoginAsync("Google", "google-user-id"))
            .ReturnsAsync(new ApplicationUser { Id = "user-1", Email = "test@example.com" });

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/grupos", redirect.Url);

        // AppLog.UserId is a FK to AspNetUsers: writing the Google provider key
        // there throws in Postgres and turns the second login into a 500.
        await using var db = dbFactory.Object.CreateDbContext();
        var audit = Assert.Single(db.Logs.Where(l => l.EventType == AuditEvents.UserLoginSuccess));
        Assert.Equal("user-1", audit.UserId);
        Assert.Equal("user-1", audit.EntityId);
    }

    [Fact]
    public async Task OnGetCallbackAsync_ExistingEmail_LinksLoginAndSignsIn()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        var existingUser = new ApplicationUser
        {
            Id = "user-1",
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = false
        };

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(existingUser);
        userManager.Setup(x => x.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/grupos", redirect.Url);
        signInManager.Verify(x => x.SignInAsync(existingUser, false, null), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_NewEmail_CreatesUserWithEmailConfirmedAndSignsIn()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "user")).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/grupos", redirect.Url);

        userManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == "test@example.com" && u.UserName == "test@example.com" && u.EmailConfirmed)), Times.Once);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "user"), Times.Once);
        signInManager.Verify(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_NoEmail_RedirectsToLogin()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _, email: null);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
    }

    [Fact]
    public async Task OnGetCallbackAsync_UnverifiedEmail_DoesNotLinkToExistingAccount()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _, emailVerified: "false");

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        var existingUser = new ApplicationUser
        {
            Id = "user-1",
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(existingUser);

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        userManager.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Never);
        signInManager.Verify(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetCallbackAsync_UnverifiedEmail_CreatesUserUnconfirmed()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _, emailVerified: "false");

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "user")).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        await model.OnGetCallbackAsync("/grupos");

        userManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => !u.EmailConfirmed)), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_VerifiedEmail_LinksToExistingAccount()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _, emailVerified: "true");

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        var existingUser = new ApplicationUser { Id = "user-1", Email = "test@example.com" };
        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(existingUser);
        userManager.Setup(x => x.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        var result = await model.OnGetCallbackAsync("/grupos");

        Assert.IsType<LocalRedirectResult>(result);
        signInManager.Verify(x => x.SignInAsync(existingUser, false, null), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_AddLoginFailsOnNewUser_DeletesUserAndRedirects()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "user")).ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Login already associated" }));
        userManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        userManager.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        signInManager.Verify(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetCallbackAsync_CreateFails_RedirectsToLogin()
    {
        var model = CreateModel(out var signInManager, out var userManager, out _);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "google-user-id", false, true))
            .ReturnsAsync(SignInResult.Failed);

        userManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Create failed" }));

        var result = await model.OnGetCallbackAsync("/grupos");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        }
    }
}
