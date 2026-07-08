using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Shared;

namespace Confirmai.Tests;

public class CityServiceTests
{
    private static Mock<IDbContextFactory<AppDbContext>> CreateFactoryMock(AppDbContext db)
    {
        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        factoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        return factoryMock;
    }

    [Fact]
    public async Task GetCityOptions_WithActiveGroups_ReturnsDistinctCities()
    {
        var db = TestDataFactory.CreateDbContext();
        db.Groups.Add(new Group { Name = "G1", Sport = Sport.Futsal, City = "Sao Paulo", StateCode = "SP", IsActive = true });
        db.Groups.Add(new Group { Name = "G2", Sport = Sport.Futsal, City = "Sao Paulo", StateCode = "SP", IsActive = true });
        db.Groups.Add(new Group { Name = "G3", Sport = Sport.Futsal, City = "Belo Horizonte", StateCode = "MG", IsActive = true });
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new CityService(factoryMock.Object);

        var options = await service.GetCityOptionsAsync();

        Assert.Equal(2, options.Count);
        var sp = options.First(o => o.City == "Sao Paulo");
        Assert.Equal("Sao Paulo|SP", sp.Key);
        Assert.Equal("Sao Paulo - SP", sp.Label);
        Assert.Equal("SP", sp.StateCode);
    }

    [Fact]
    public async Task GetCityOptions_NoActiveGroups_ReturnsEmptyList()
    {
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new CityService(factoryMock.Object);

        var options = await service.GetCityOptionsAsync();

        Assert.Empty(options);
    }

    [Fact]
    public async Task GetCityOptions_InactiveGroupsExcluded()
    {
        var db = TestDataFactory.CreateDbContext();
        db.Groups.Add(new Group { Name = "G1", Sport = Sport.Futsal, City = "Ativa", StateCode = "SP", IsActive = true });
        db.Groups.Add(new Group { Name = "G2", Sport = Sport.Futsal, City = "Inativa", StateCode = "MG", IsActive = false });
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new CityService(factoryMock.Object);

        var options = await service.GetCityOptionsAsync();

        Assert.Single(options);
        Assert.Equal("Ativa", options[0].City);
    }

    [Fact]
    public async Task GetIbgeMunicipios_EmptyStateCode_ReturnsEmpty()
    {
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new CityService(factoryMock.Object);

        var result = await service.GetIbgeMunicipiosAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetIbgeMunicipios_NullStateCode_ReturnsEmpty()
    {
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new CityService(factoryMock.Object);

        var result = await service.GetIbgeMunicipiosAsync(null!);

        Assert.Empty(result);
    }
}
