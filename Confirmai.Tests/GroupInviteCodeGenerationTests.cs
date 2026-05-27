using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class GroupInviteCodeGenerationTests
{
    [Fact]
    public void SaveChanges_GeneratesInviteCode_WhenMissing()
    {
        using var db = CreateDbContext();

        var group = new Group
        {
            Name = "Grupo sem codigo",
            Sport = Sport.Futsal,
            CreatedByUserId = "u1",
            InviteCode = null,
        };

        db.Groups.Add(group);
        db.SaveChanges();

        Assert.False(string.IsNullOrWhiteSpace(group.InviteCode));
        Assert.Equal(8, group.InviteCode!.Length);
    }

    [Fact]
    public void SaveChanges_PreservesInviteCode_WhenProvided()
    {
        using var db = CreateDbContext();

        var group = new Group
        {
            Name = "Grupo com codigo",
            Sport = Sport.Futsal,
            CreatedByUserId = "u1",
            InviteCode = "ABCD1234",
        };

        db.Groups.Add(group);
        db.SaveChanges();

        Assert.Equal("ABCD1234", group.InviteCode);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
