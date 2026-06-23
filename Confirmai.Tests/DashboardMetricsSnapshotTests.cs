using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class DashboardMetricsSnapshotTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var usersCount = 100;
        var quoteQueriesCount = 500;

        // Act
        var snapshot = new DashboardMetricsSnapshot
        {
            UsersCount = usersCount,
            QuoteQueriesCount = quoteQueriesCount
        };

        // Assert
        Assert.Equal(usersCount, snapshot.UsersCount);
        Assert.Equal(quoteQueriesCount, snapshot.QuoteQueriesCount);
    }

    [Fact]
    public void Constructor_WithZeroValues()
    {
        // Arrange
        var usersCount = 0;
        var quoteQueriesCount = 0;

        // Act
        var snapshot = new DashboardMetricsSnapshot
        {
            UsersCount = usersCount,
            QuoteQueriesCount = quoteQueriesCount
        };

        // Assert
        Assert.Equal(0, snapshot.UsersCount);
        Assert.Equal(0, snapshot.QuoteQueriesCount);
    }

    [Fact]
    public void Constructor_WithLargeValues()
    {
        // Arrange
        var usersCount = 10000;
        var quoteQueriesCount = 50000;

        // Act
        var snapshot = new DashboardMetricsSnapshot
        {
            UsersCount = usersCount,
            QuoteQueriesCount = quoteQueriesCount
        };

        // Assert
        Assert.Equal(10000, snapshot.UsersCount);
        Assert.Equal(50000, snapshot.QuoteQueriesCount);
    }
}
