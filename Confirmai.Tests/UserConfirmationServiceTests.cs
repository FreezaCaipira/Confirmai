using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class UserConfirmationServiceTests
{
    private readonly UserConfirmationService _service;

    public UserConfirmationServiceTests()
    {
        _service = new UserConfirmationService(null!);
    }

    [Fact]
    public void FilterBySport_WithNullSport_ReturnsAllConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterBySport(confirmations, null);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterBySport_WithFutsal_ReturnsOnlyFutsalConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterBySport(confirmations, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Equal(Sport.Futsal, result[0].Event.Sport);
    }

    [Fact]
    public void FilterBySport_WithPoker_ReturnsOnlyPokerConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterBySport(confirmations, Sport.Poker);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(Sport.Poker, c.Event.Sport));
    }

    [Fact]
    public void FilterByPeriod_WithAll_ReturnsAllConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterByPeriod(confirmations, "all");

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterByPeriod_With7Days_ReturnsRecentConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterByPeriod(confirmations, "7days");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterByPeriod_With30Days_ReturnsConfirmationsWithin30Days()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.FilterByPeriod(confirmations, "30days");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyFilters_WithSportAndPeriod_AppliesBothFilters()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.ApplyFilters(confirmations, Sport.Poker, "7days");

        // Assert
        Assert.Single(result);
        Assert.Equal(Sport.Poker, result[0].Event.Sport);
    }

    [Fact]
    public void ApplyFilters_WithNullFilters_ReturnsAllConfirmations()
    {
        // Arrange
        var confirmations = CreateTestConfirmations();

        // Act
        var result = _service.ApplyFilters(confirmations, null, "all");

        // Assert
        Assert.Equal(3, result.Count);
    }

    private static List<EventConfirmation> CreateTestConfirmations()
    {
        var now = DateTime.UtcNow;
        var group = new Group { Name = "Test Group" };

        return new List<EventConfirmation>
        {
            new()
            {
                Event = new Event
                {
                    Sport = Sport.Futsal,
                    StartsAt = now.AddDays(1),
                    Group = group,
                    Confirmations = new List<EventConfirmation>()
                }
            },
            new()
            {
                Event = new Event
                {
                    Sport = Sport.Poker,
                    StartsAt = now.AddDays(3),
                    Group = group,
                    Confirmations = new List<EventConfirmation>()
                }
            },
            new()
            {
                Event = new Event
                {
                    Sport = Sport.Poker,
                    StartsAt = now.AddDays(-40),
                    Group = group,
                    Confirmations = new List<EventConfirmation>()
                }
            }
        };
    }
}
