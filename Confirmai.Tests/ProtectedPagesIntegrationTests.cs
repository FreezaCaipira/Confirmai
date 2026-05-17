using System.Net;

namespace Confirmai.Tests;

public class ProtectedPagesIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private static readonly string[] NotAuthorizedTexts =
    {
        "Voce nao tem permissao para acessar esta pagina.",
        "You are not authorized to access this page.",
        "No tienes permiso para acceder a esta pagina."
    };

    private static readonly string[] AdminPanelTitles =
    {
        "Painel Administrativo",
        "Admin Panel",
        "Panel Administrativo"
    };

    private readonly IntegrationTestWebAppFactory _factory;

    public ProtectedPagesIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private static readonly string[] LoginRedirectIndicators =
    {
        "Identity/Account/Login",
        "Login",
        "Entrar"
    };

    [Fact]
    public async Task AdminPage_ShowsNotAuthorized_ForNonAdminUser()
    {
        var client = CreateAuthenticatedClient(userId: "user-int", userName: "user", roles: "user");
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, NotAuthorizedTexts);
    }

    [Fact]
    public async Task AdminPage_RendersForAdminRole()
    {
        var client = CreateAuthenticatedClient(userId: "admin-int", userName: "admin", roles: "admin");
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, AdminPanelTitles);
    }

    [Fact]
    public async Task AdminPage_ShowsNotAuthorized_WhenRoleHeaderIsBlank()
    {
        var client = CreateHeaderClient(("X-Test-UserId", "user-int-blank-role"), ("X-Test-UserName", "userblankrole"), ("X-Test-Roles", "   "));
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, NotAuthorizedTexts);
    }

    [Fact]
    public async Task AdminPage_RendersForMixedRoleHeader_WhenAdminIsPresent()
    {
        var client = CreateHeaderClient(("X-Test-UserId", "admin-int-mixed-role"), ("X-Test-UserName", "adminmixedrole"), ("X-Test-Roles", "user, ,admin, "));
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, AdminPanelTitles);
    }

    [Fact]
    public async Task AdminPage_ShowsNotAuthorized_ForMixedRoleHeader_WithoutAdmin()
    {
        var client = CreateHeaderClient(("X-Test-UserId", "user-int-mixed-no-admin"), ("X-Test-UserName", "usermixednoadmin"), ("X-Test-Roles", "user, ,manager, "));
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, NotAuthorizedTexts);
    }

    [Fact]
    public async Task AdminPage_RendersWhenAdminRoleIsDuplicated()
    {
        var client = CreateHeaderClient(("X-Test-UserId", "admin-int-dup-role"), ("X-Test-UserName", "admindupe"), ("X-Test-Roles", "admin,admin"));
        var result = await GetPageAsync(client, "/admin");
        AssertOkAndContainsAny(result, AdminPanelTitles);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    [InlineData("/admin/users/view/user-123")]
    [InlineData("/admin/users/edit/user-123")]
    [InlineData("/admin/payments")]
    [InlineData("/admin/gateways")]
    [InlineData("/admin/venues")]
    [InlineData("/admin/venues/edit/0")]
    [InlineData("/admin/security")]
    [InlineData("/admin/languages")]
    [InlineData("/admin/logs")]
    public async Task AdminRoutes_ShowNotAuthorized_ForNonAdminUser(string route)
    {
        var client = CreateAuthenticatedClient(userId: "user-int-admin-surface", userName: "user", roles: "user");
        var result = await GetPageAsync(client, route);
        AssertOkAndContainsAny(result, NotAuthorizedTexts);
    }

    [Theory]
    [InlineData("/payments/view/1")]
    public async Task AuthenticatedRoutes_ShowNotAuthorized_WhenAnonymous(string route)
    {
        var result = await GetPageAsync(_factory.CreateClient(), route);
        AssertOkAndContainsAny(result, LoginRedirectIndicators);
    }

    [Theory]
    [InlineData("/about")]
    [InlineData("/contact")]
    [InlineData("/marketplace")]
    [InlineData("/profile/non-existent-user")]
    [InlineData("/delivery-agents/999999")]
    public async Task PublicRoutes_AreAccessible_WhenAnonymous(string route)
    {
        var result = await GetPageAsync(_factory.CreateClient(), route);
        Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string userId, string userName, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", userName);

        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', roles));

        return client;
    }

    private HttpClient CreateHeaderClient(params (string Name, string Value)[] headers)
    {
        var client = _factory.CreateClient();
        foreach (var (name, value) in headers)
            client.DefaultRequestHeaders.Add(name, value);

        return client;
    }

    private async Task<(HttpResponseMessage Response, string Html)> GetPageAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        return (response, html);
    }

    private static void AssertOkAndContainsAny((HttpResponseMessage Response, string Html) result, params string[] expectedTexts)
    {
        Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
        Assert.True(
            expectedTexts.Any(expectedText => result.Html.Contains(expectedText, StringComparison.Ordinal)),
            $"Expected one of [{string.Join(" | ", expectedTexts)}] in HTML response.");
    }

}


