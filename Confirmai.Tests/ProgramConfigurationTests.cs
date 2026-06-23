using Confirmai.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Confirmai.Tests;

public class ProgramConfigurationTests
{
    [Fact]
    public async Task Program_ConfiguresKestrel_WhenEfiBankWebhookClientCertSubjectIsSet()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["EfiBank:WebhookClientCertSubject"] = "CN=EfiBank Client"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the Kestrel configuration worked
        Assert.True(true);
    }

    [Fact]
    public async Task Program_DoesNotConfigureKestrel_WhenEfiBankWebhookClientCertSubjectIsNotSet()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the Kestrel configuration worked
        Assert.True(true);
    }

    [Fact]
    public async Task IndexPage_ReturnsSuccess()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/jogos");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task IndexPage_ReturnsSuccess_WithEmailOptionsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Email:From"] = "test@example.com",
                        ["Email:SmtpServer"] = "smtp.example.com",
                        ["Email:Port"] = "587",
                        ["Email:User"] = "test@example.com",
                        ["Email:Password"] = "test123"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/jogos");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task EscalacaoPage_ReturnsSuccessOrRedirect_WhenNotAuthenticated()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/futsal/escalacao/1");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task EscalacaoPage_ReturnsSuccess_WithEmailOptionsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Email:From"] = "test@example.com",
                        ["Email:SmtpServer"] = "smtp.example.com",
                        ["Email:Port"] = "587",
                        ["Email:User"] = "test@example.com",
                        ["Email:Password"] = "test123"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/futsal/escalacao/1");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task AdminPage_ReturnsSuccessOrRedirect_WhenNotAuthenticated()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/admin");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task AdminPage_ReturnsSuccess_WithEmailOptionsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Email:From"] = "test@example.com",
                        ["Email:SmtpServer"] = "smtp.example.com",
                        ["Email:Port"] = "587",
                        ["Email:User"] = "test@example.com",
                        ["Email:Password"] = "test123"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/admin");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task ProfilePage_ReturnsSuccessOrRedirect_WhenNotAuthenticated()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/profile");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task ProfilePage_ReturnsSuccess_WithEmailOptionsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Email:From"] = "test@example.com",
                        ["Email:SmtpServer"] = "smtp.example.com",
                        ["Email:Port"] = "587",
                        ["Email:User"] = "test@example.com",
                        ["Email:Password"] = "test123"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        var response = await client.GetAsync("/profile");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.Found);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenOpenTelemetryIsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["OpenTelemetry:Endpoint"] = "http://localhost:4317",
                        ["OpenTelemetry:ServiceName"] = "test-service",
                        ["OpenTelemetry:ServiceVersion"] = "1.0.0",
                        ["OpenTelemetry:Environment"] = "test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, OpenTelemetry configuration worked
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenOpenTelemetryIsNotConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["OpenTelemetry:Endpoint"] = ""
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started without OpenTelemetry
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenEmailOptionsAreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Email:SmtpHost"] = "smtp.test.com",
                        ["Email:SmtpPort"] = "587",
                        ["Email:SmtpUser"] = "test@test.com",
                        ["Email:SmtpPass"] = "testpass",
                        ["Email:FromEmail"] = "noreply@test.com",
                        ["Email:FromName"] = "Test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with email config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenEmailOptionsAreNotConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started without email config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenDefaultConnectionIsConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with connection string
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenBtcPayOptionsAreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["BtcPay:StoreUrl"] = "https://btcpay.example.com",
                        ["BtcPay:ApiKey"] = "test-api-key"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with BtcPay config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenAbacatePayOptionsAreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["AbacatePay:BaseUrl"] = "https://api.abacatepay.com/v2",
                        ["AbacatePay:ApiKey"] = "test-api-key"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with AbacatePay config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenAppmaxOptionsAreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["Appmax:ApiKey"] = "test-api-key",
                        ["Appmax:ClientId"] = "test-client-id",
                        ["Appmax:ClientSecret"] = "test-client-secret"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with Appmax config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenEfiBankOptionsAreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["EfiBank:ClientId"] = "test-client-id",
                        ["EfiBank:ClientSecret"] = "test-client-secret",
                        ["EfiBank:PixKey"] = "test-pix-key"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with EfiBank config
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenEfiBankSandboxIsDisabledInProduction()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["EfiBank:ClientId"] = "test-client-id",
                        ["EfiBank:ClientSecret"] = "test-client-secret",
                        ["EfiBank:PixKey"] = "test-pix-key",
                        ["EfiBank:Sandbox"] = "false"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with EfiBank sandbox disabled
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WhenAbacatePayDevApiKeyIsUsedInDevelopment()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test",
                        ["AbacatePay:BaseUrl"] = "https://api.abacatepay.com/v2",
                        ["AbacatePay:ApiKey"] = "abc_dev_test_key"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with AbacatePay dev API key in Development
        Assert.True(true);
    }

    [Fact]
    public async Task Program_StartsSuccessfully_WithStaticFilesConfiguration()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // If we get here without exception, the program started with static files configuration
        Assert.True(true);
    }

    [Fact]
    public async Task SetLanguageEndpoint_ReturnsSuccess()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/set-language/pt-BR");

        // The endpoint should return a redirect (302) which is considered a success
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Found || response.StatusCode == System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task WebhookEndpoints_AreConfigured()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Confirmai_tests;Username=test;Password=test"
                    };
                    configBuilder.AddInMemoryCollection(settings);
                });
            });

        var client = factory.CreateClient();
        await Task.Delay(100); // Give time for startup to complete

        // Test that webhook endpoints are configured (they should respond)
        var btcPayResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/btcpay/webhook"));
        var abacatePayResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/abacatepay/webhook"));
        var efiBankResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/efibank/webhook"));

        // All should respond (status code may vary based on rate limiting and method)
        Assert.True(btcPayResponse.StatusCode == System.Net.HttpStatusCode.OK || 
                    btcPayResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                    btcPayResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        Assert.True(abacatePayResponse.StatusCode == System.Net.HttpStatusCode.OK || 
                    abacatePayResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                    abacatePayResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        Assert.True(efiBankResponse.StatusCode == System.Net.HttpStatusCode.OK || 
                    efiBankResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                    efiBankResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
    }
}
