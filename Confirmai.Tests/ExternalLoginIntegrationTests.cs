using System.Net;

namespace Confirmai.Tests;

public class ExternalLoginIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ExternalLoginIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Page_Renders_Google_Button_When_Configured()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientId", "test-client");
        Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientSecret", "test-secret");

        try
        {
            using var customFactory = _factory.WithWebHostBuilder(_ => { });
            var client = customFactory.CreateClient();
            var response = await client.GetAsync("/Identity/Account/Login");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("google-signin-btn", html);
            Assert.Contains("Continuar com o Google", html);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientId", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientSecret", null);
        }
    }

    [Fact]
    public async Task Register_Page_Renders_Google_Button_When_Configured()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientId", "test-client");
        Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientSecret", "test-secret");

        try
        {
            using var customFactory = _factory.WithWebHostBuilder(_ => { });
            var client = customFactory.CreateClient();
            var response = await client.GetAsync("/Identity/Account/Register");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("google-signin-btn", html);
            Assert.Contains("name=\"provider\" value=\"Google\"", html);
            Assert.Contains("Continuar com o Google", html);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientId", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientSecret", null);
        }
    }

    [Fact]
    public async Task Register_Page_Hides_Google_Button_When_Not_Configured()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("google-signin-btn", html);
    }
}
