using System.Security.Claims;
using Confirmai.Models;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Tests for ExternalLoginClaimsExtractor (C30-B Fase 1).
/// Covers: claim present populates, claim absent doesn't break,
/// download failure doesn't block, existing FullName preserved.
/// </summary>
public class ExternalLoginClaimsExtractorTests
{
    private static ClaimsPrincipal BuildPrincipal(
        string? email = null,
        string? name = null,
        string? givenName = null,
        string? surname = null,
        string? picture = null)
    {
        var claims = new List<Claim>();
        if (email is not null) claims.Add(new Claim(ClaimTypes.Email, email));
        if (name is not null) claims.Add(new Claim(ClaimTypes.Name, name));
        if (givenName is not null) claims.Add(new Claim(ClaimTypes.GivenName, givenName));
        if (surname is not null) claims.Add(new Claim(ClaimTypes.Surname, surname));
        if (picture is not null) claims.Add(new Claim("picture", picture));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ExternalLoginClaimsExtractor CreateExtractor(HttpClient? http = null)
    {
        var env = new TestWebHostEnvironment();
        http ??= new HttpClient(new StubHttpMessageHandler());
        return new ExternalLoginClaimsExtractor(env, http);
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
        public bool ShouldFail { get; set; }
        public byte[] ResponseBytes { get; set; } = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldFail)
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("network error"));

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ResponseBytes)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        }
    }

    // ── ExtractFullName ──

    [Fact]
    public void ExtractFullName_ClaimTypesName_Populates()
    {
        var principal = BuildPrincipal(name: "João Silva");
        var result = ExternalLoginClaimsExtractor.ExtractFullName(principal);
        Assert.Equal("João Silva", result);
    }

    [Fact]
    public void ExtractFullName_GivenAndSurname_FallsBackWhenNoName()
    {
        var principal = BuildPrincipal(givenName: "João", surname: "Silva");
        var result = ExternalLoginClaimsExtractor.ExtractFullName(principal);
        Assert.Equal("João Silva", result);
    }

    [Fact]
    public void ExtractFullName_NoClaims_ReturnsNull()
    {
        var principal = BuildPrincipal(email: "test@example.com");
        var result = ExternalLoginClaimsExtractor.ExtractFullName(principal);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFullName_OnlyGivenName_Works()
    {
        var principal = BuildPrincipal(givenName: "João");
        var result = ExternalLoginClaimsExtractor.ExtractFullName(principal);
        Assert.Equal("João", result);
    }

    [Fact]
    public void ExtractFullName_NameWithExtraWhitespace_Trims()
    {
        var principal = BuildPrincipal(name: "  João Silva  ");
        var result = ExternalLoginClaimsExtractor.ExtractFullName(principal);
        Assert.Equal("João Silva", result);
    }

    // ── ExtractPictureUrl ──

    [Fact]
    public void ExtractPictureUrl_Present_ReturnsUrl()
    {
        var principal = BuildPrincipal(picture: "https://lh3.googleusercontent.com/photo.jpg");
        var result = ExternalLoginClaimsExtractor.ExtractPictureUrl(principal);
        Assert.Equal("https://lh3.googleusercontent.com/photo.jpg", result);
    }

    [Fact]
    public void ExtractPictureUrl_Absent_ReturnsNull()
    {
        var principal = BuildPrincipal(email: "test@example.com");
        var result = ExternalLoginClaimsExtractor.ExtractPictureUrl(principal);
        Assert.Null(result);
    }

    // ── ApplyClaimsAsync: new account ──

    [Fact]
    public async Task ApplyClaimsAsync_NewAccount_FillsFullNameAndAvatar()
    {
        var extractor = CreateExtractor();
        var user = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" };
        var principal = BuildPrincipal(
            email: "test@example.com",
            name: "João Silva",
            picture: "https://example.com/photo.jpg");

        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: false);

        Assert.Equal("João Silva", user.FullName);
        Assert.NotNull(user.AvatarPath);
        Assert.StartsWith("/uploads/avatars/", user.AvatarPath);
    }

    [Fact]
    public async Task ApplyClaimsAsync_NewAccount_NoClaims_DoesNotBreak()
    {
        var extractor = CreateExtractor();
        var user = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" };
        var principal = BuildPrincipal(email: "test@example.com");

        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: false);

        Assert.Null(user.FullName);
        Assert.Null(user.AvatarPath);
    }

    // ── ApplyClaimsAsync: existing account (link) ──

    [Fact]
    public async Task ApplyClaimsAsync_ExistingAccount_PreservesFullName()
    {
        var extractor = CreateExtractor();
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = "Nome Editado Pelo Usuario"
        };
        var principal = BuildPrincipal(
            email: "test@example.com",
            name: "João Silva",
            picture: "https://example.com/photo.jpg");

        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: true);

        Assert.Equal("Nome Editado Pelo Usuario", user.FullName);
    }

    [Fact]
    public async Task ApplyClaimsAsync_ExistingAccount_DoesNotDownloadAvatar()
    {
        var extractor = CreateExtractor();
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com"
        };
        var principal = BuildPrincipal(
            email: "test@example.com",
            name: "João Silva",
            picture: "https://example.com/photo.jpg");

        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: true);

        // Existing account: avatar is NOT downloaded even if empty
        Assert.Null(user.AvatarPath);
        // But FullName IS filled if empty (the user never set one)
        Assert.Equal("João Silva", user.FullName);
    }

    [Fact]
    public async Task ApplyClaimsAsync_ExistingAccount_EmptyFullName_GetsFilled()
    {
        var extractor = CreateExtractor();
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = null
        };
        var principal = BuildPrincipal(name: "João Silva");

        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: true);

        Assert.Equal("João Silva", user.FullName);
    }

    // ── Download failure ──

    [Fact]
    public async Task ApplyClaimsAsync_DownloadFails_UserStillLogsIn()
    {
        var handler = new StubHttpMessageHandler { ShouldFail = true };
        var http = new HttpClient(handler);
        var extractor = CreateExtractor(http);
        var user = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" };
        var principal = BuildPrincipal(
            email: "test@example.com",
            name: "João Silva",
            picture: "https://example.com/broken.jpg");

        // Must not throw
        await extractor.ApplyClaimsAsync(user, principal, isExistingAccount: false);

        Assert.Equal("João Silva", user.FullName);
        Assert.Null(user.AvatarPath); // download failed, avatar stays null
    }

    [Fact]
    public async Task DownloadAvatarAsync_NullUrl_ReturnsNull()
    {
        var extractor = CreateExtractor();
        var result = await extractor.DownloadAvatarAsync(null);
        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAvatarAsync_EmptyUrl_ReturnsNull()
    {
        var extractor = CreateExtractor();
        var result = await extractor.DownloadAvatarAsync("");
        Assert.Null(result);
    }
}
