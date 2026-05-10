using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class ServerRegistrationRequestServiceTests
{
    private static (ServerRegistrationRequestService service, AppDbContext db) CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new AppDbContext(options);
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        return (new ServerRegistrationRequestService(db, env.Object), db);
    }

    [Fact]
    public async Task CreateAsync_PersistsRequest_WithPendingStatus()
    {
        var (service, db) = CreateService();

        var input = new ServerRegistrationRequestService.CreateServerRegistrationRequestInput
        {
            Name = "  TestServer  ",
            TibiaVersion = " 7.72 ",
            WebsiteUrl = "https://example.com",
            Region = "Brazil",
            RequestNote = "My note"
        };

        var result = await service.CreateAsync(input, "user-1");

        Assert.Equal("TestServer", result.RequestedName);
        Assert.Equal("7.72", result.TibiaVersion);
        Assert.Equal("https://example.com", result.WebsiteUrl);
        Assert.Contains("Brazil", result.RequestNote!);
        Assert.Contains("My note", result.RequestNote!);
        Assert.Equal(ServerRegistrationRequestStatus.Pending, result.Status);
        Assert.Equal("user-1", result.RequesterUserId);
        Assert.True(await db.ServerRegistrationRequests.AnyAsync(r => r.Id == result.Id));
    }

    [Fact]
    public async Task CreateAsync_WithNullOptionalFields_SetsNulls()
    {
        var (service, db) = CreateService();

        var input = new ServerRegistrationRequestService.CreateServerRegistrationRequestInput
        {
            Name = "Server",
            TibiaVersion = "8.60"
        };

        var result = await service.CreateAsync(input, "user-2");

        Assert.Null(result.WebsiteUrl);
        Assert.Null(result.RequestNote);
        Assert.Null(result.RequestedLogoPath);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingRequests()
    {
        var (service, db) = CreateService();

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user1" });
        db.ServerRegistrationRequests.AddRange(
            new ServerRegistrationRequest { RequestedName = "A", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Pending },
            new ServerRegistrationRequest { RequestedName = "B", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Approved },
            new ServerRegistrationRequest { RequestedName = "C", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Pending }
        );
        await db.SaveChangesAsync();

        var pending = await service.GetPendingAsync();

        Assert.Equal(2, pending.Count);
        Assert.All(pending, r => Assert.Equal(ServerRegistrationRequestStatus.Pending, r.Status));
    }

    [Fact]
    public async Task GetRecentReviewedAsync_ExcludesPending_OrdersByReviewedAtDesc()
    {
        var (service, db) = CreateService();

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user1" });
        db.ServerRegistrationRequests.AddRange(
            new ServerRegistrationRequest { RequestedName = "Pending", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Pending },
            new ServerRegistrationRequest { RequestedName = "OlderReview", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Rejected, ReviewedAt = DateTime.UtcNow.AddDays(-2) },
            new ServerRegistrationRequest { RequestedName = "NewerReview", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Approved, ReviewedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await db.SaveChangesAsync();

        var reviewed = await service.GetRecentReviewedAsync();

        Assert.Equal(2, reviewed.Count);
        Assert.Equal("NewerReview", reviewed[0].RequestedName);
        Assert.Equal("OlderReview", reviewed[1].RequestedName);
    }

    [Fact]
    public async Task CountPendingAsync_ReturnsCorrectCount()
    {
        var (service, db) = CreateService();

        db.ServerRegistrationRequests.AddRange(
            new ServerRegistrationRequest { RequestedName = "A", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Pending },
            new ServerRegistrationRequest { RequestedName = "B", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Rejected },
            new ServerRegistrationRequest { RequestedName = "C", TibiaVersion = "7.72", RequesterUserId = "u1", Status = ServerRegistrationRequestStatus.Pending }
        );
        await db.SaveChangesAsync();

        Assert.Equal(2, await service.CountPendingAsync());
    }

    [Fact]
    public async Task ApproveAsync_CreatesServerAndMember_WhenValid()
    {
        var (service, db) = CreateService();

        db.Users.Add(new ApplicationUser { Id = "req-user", UserName = "requester" });
        db.ServerRegistrationRequests.Add(new ServerRegistrationRequest
        {
            Id = 1,
            RequestedName = "NewServer",
            TibiaVersion = "7.72",
            RequesterUserId = "req-user",
            Status = ServerRegistrationRequestStatus.Pending,
            RequestNote = "Regiao: Brazil"
        });
        await db.SaveChangesAsync();

        var result = await service.ApproveAsync(1, "admin-user", "Looks good");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ServerId);

        var server = await db.Servers.FindAsync(result.ServerId);
        Assert.NotNull(server);
        Assert.Equal("NewServer", server!.Name);
        Assert.Equal("7.72", server.TibiaVersion);
        Assert.Equal("Brazil", server.Region);
        Assert.True(server.IsActive);
        Assert.Equal("req-user", server.PrimaryGameMasterUserId);

        var member = await db.ServerMembers.FirstOrDefaultAsync(m => m.ServerId == server.Id);
        Assert.NotNull(member);
        Assert.Equal("req-user", member!.UserId);
        Assert.Equal(ServerMemberRole.ServerAdmin, member.Role);

        var request = await db.ServerRegistrationRequests.FindAsync(1);
        Assert.Equal(ServerRegistrationRequestStatus.Approved, request!.Status);
        Assert.Equal("admin-user", request.ReviewerUserId);
        Assert.Equal("Looks good", request.ReviewNote);
        Assert.NotNull(request.ReviewedAt);
        Assert.Equal(server.Id, request.ApprovedServerId);
    }

    [Fact]
    public async Task ApproveAsync_Fails_WhenRequestNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.ApproveAsync(999, "admin", null);

        Assert.False(result.Succeeded);
        Assert.Contains("nao encontrada", result.Message);
    }

    [Fact]
    public async Task ApproveAsync_Fails_WhenRequestAlreadyReviewed()
    {
        var (service, db) = CreateService();

        db.ServerRegistrationRequests.Add(new ServerRegistrationRequest
        {
            Id = 1,
            RequestedName = "Server",
            TibiaVersion = "7.72",
            RequesterUserId = "u1",
            Status = ServerRegistrationRequestStatus.Rejected
        });
        await db.SaveChangesAsync();

        var result = await service.ApproveAsync(1, "admin", null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ApproveAsync_Fails_WhenDuplicateServerExists()
    {
        var (service, db) = CreateService();

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user1" });
        db.Servers.Add(new TibiaServer { Name = "ExistingServer", TibiaVersion = "7.72", IsActive = true });
        db.ServerRegistrationRequests.Add(new ServerRegistrationRequest
        {
            Id = 1,
            RequestedName = "ExistingServer",
            TibiaVersion = "7.72",
            RequesterUserId = "u1",
            Status = ServerRegistrationRequestStatus.Pending
        });
        await db.SaveChangesAsync();

        var result = await service.ApproveAsync(1, "admin", null);

        Assert.False(result.Succeeded);
        Assert.Contains("Ja existe", result.Message);
    }

    [Fact]
    public async Task RejectAsync_SetsRejectedStatus_WhenValid()
    {
        var (service, db) = CreateService();

        db.ServerRegistrationRequests.Add(new ServerRegistrationRequest
        {
            Id = 1,
            RequestedName = "Server",
            TibiaVersion = "7.72",
            RequesterUserId = "u1",
            Status = ServerRegistrationRequestStatus.Pending
        });
        await db.SaveChangesAsync();

        var result = await service.RejectAsync(1, "admin-user", "Not suitable");

        Assert.True(result.Succeeded);

        var request = await db.ServerRegistrationRequests.FindAsync(1);
        Assert.Equal(ServerRegistrationRequestStatus.Rejected, request!.Status);
        Assert.Equal("admin-user", request.ReviewerUserId);
        Assert.Equal("Not suitable", request.ReviewNote);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public async Task RejectAsync_UsesDefaultNote_WhenNullProvided()
    {
        var (service, db) = CreateService();

        db.ServerRegistrationRequests.Add(new ServerRegistrationRequest
        {
            Id = 1,
            RequestedName = "Server",
            TibiaVersion = "7.72",
            RequesterUserId = "u1",
            Status = ServerRegistrationRequestStatus.Pending
        });
        await db.SaveChangesAsync();

        await service.RejectAsync(1, "admin", null);

        var request = await db.ServerRegistrationRequests.FindAsync(1);
        Assert.Equal("Rejeitado pela administracao.", request!.ReviewNote);
    }

    [Fact]
    public async Task RejectAsync_Fails_WhenRequestNotFound()
    {
        var (service, _) = CreateService();

        var result = await service.RejectAsync(999, "admin", null);

        Assert.False(result.Succeeded);
    }
}
