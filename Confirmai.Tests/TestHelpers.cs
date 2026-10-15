using Confirmai.Data;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext? _sharedDbContext;

    public TestDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
        _sharedDbContext = null;
    }

    public TestDbContextFactory(AppDbContext sharedDbContext)
    {
        _sharedDbContext = sharedDbContext;
        _options = new DbContextOptionsBuilder<AppDbContext>().Options;
    }

    public AppDbContext CreateDbContext()
    {
        return _sharedDbContext ?? new AppDbContext(_options);
    }

    public static IDbContextFactory<AppDbContext> CreateInMemoryFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TestDbContextFactory(options);
    }
}

/// <summary>
/// Convenience factory for a LogService backed by the same test DbContext
/// factory — lets services that require LogService run in unit tests and
/// keeps audit rows queryable afterwards.
/// </summary>
public static class TestLogService
{
    public static Confirmai.Services.Core.LogService Create(IDbContextFactory<AppDbContext> factory) =>
        new(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance);
}
