using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Confirmai.Tests;

public class PingControllerTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PingControllerTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ping_WithAuthenticatedUser_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", "test-user-1");
        client.DefaultRequestHeaders.Add("X-Test-UserName", "Test User");

        var response = await client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_WithoutAuthentication_Returns401OrRedirect()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/ping");

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected 401 or 302, got {(int)response.StatusCode} {response.StatusCode}");
    }
}
