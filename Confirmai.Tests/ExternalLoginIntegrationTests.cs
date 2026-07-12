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
            Assert.Contains("Entrar com Google", html);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientId", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_Authentication__Google__ClientSecret", null);
        }
    }
}
