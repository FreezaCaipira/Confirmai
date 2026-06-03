using Confirmai.Data;

namespace Confirmai.Tests;

public class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_Throws_WhenConfigurationIsMissingOrPlaceholder()
    {
        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "__SET_VIA_USER_SECRETS__");

        try
        {
            var factory = new AppDbContextFactory();

            var ex = Record.Exception(() => factory.CreateDbContext(Array.Empty<string>()));

            Assert.NotNull(ex);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previous);
        }
    }
}


