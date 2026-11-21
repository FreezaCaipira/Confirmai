using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;

namespace Confirmai.Tests;

public class EventAccessTests
{
    [Fact]
    public void IsCreator_ReturnsTrue_WhenUserIsCreator()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsCreator(ev, "user-1");
        
        Assert.True(result);
    }

    [Fact]
    public void IsCreator_ReturnsFalse_WhenUserIsNotCreator()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsCreator(ev, "user-2");
        
        Assert.False(result);
    }

    [Fact]
    public void IsCreator_ReturnsFalse_WhenEventIsNull()
    {
        var result = EventAccess.IsCreator(null, "user-1");
        
        Assert.False(result);
    }

    [Fact]
    public void IsCreator_ReturnsFalse_WhenUserIdIsNull()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsCreator(ev, null);
        
        Assert.False(result);
    }

    [Fact]
    public void IsCreator_ReturnsFalse_WhenUserIdIsEmpty()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsCreator(ev, "");
        
        Assert.False(result);
    }

    [Fact]
    public void IsCreator_ReturnsFalse_WhenUserIdIsWhitespace()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsCreator(ev, "   ");
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_WhenUserIsCreator()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsAdmin(ev, "user-1");
        
        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_WhenUserIsGroupAdmin()
    {
        var group = new Group();
        var user = new ApplicationUser { Id = "user-2" };
        var member = new GroupMember { UserId = "user-2", Role = GroupMemberRole.Admin, User = user };
        group.Members.Add(member);
        
        var ev = new Event { CreatedByUserId = "user-1", Group = group };
        var result = EventAccess.IsAdmin(ev, "user-2");
        
        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserIsNotCreatorAndNotAdmin()
    {
        var group = new Group();
        var user = new ApplicationUser { Id = "user-2" };
        var member = new GroupMember { UserId = "user-2", Role = GroupMemberRole.Member, User = user };
        group.Members.Add(member);
        
        var ev = new Event { CreatedByUserId = "user-1", Group = group };
        var result = EventAccess.IsAdmin(ev, "user-2");
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenEventIsNull()
    {
        var result = EventAccess.IsAdmin(null, "user-1");
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserIdIsNull()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsAdmin(ev, null);
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserIdIsEmpty()
    {
        var ev = new Event { CreatedByUserId = "user-1" };
        var result = EventAccess.IsAdmin(ev, "");
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenGroupIsNull()
    {
        var ev = new Event { CreatedByUserId = "user-1", Group = null! };
        var result = EventAccess.IsAdmin(ev, "user-2");
        
        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserNotInGroup()
    {
        var group = new Group();
        var user = new ApplicationUser { Id = "user-3" };
        var member = new GroupMember { UserId = "user-3", Role = GroupMemberRole.Admin, User = user };
        group.Members.Add(member);

        var ev = new Event { CreatedByUserId = "user-1", Group = group };
        var result = EventAccess.IsAdmin(ev, "user-2");

        Assert.False(result);
    }

    // ── C46 (1.5): CanManage = group admin OR system admin ─────────────

    private static Event EventWithGroup(GroupMemberRole? role, string memberUserId = "user-2")
    {
        var group = new Group();
        if (role is not null)
            group.Members.Add(new GroupMember { UserId = memberUserId, Role = role.Value });
        return new Event { CreatedByUserId = "creator-1", Group = group };
    }

    [Fact]
    public void CanManage_ReturnsTrue_WhenUserIsGroupAdmin()
        => Assert.True(EventAccess.CanManage(EventWithGroup(GroupMemberRole.Admin), "user-2", false));

    [Fact]
    public void CanManage_ReturnsTrue_WhenUserIsSystemAdmin()
        => Assert.True(EventAccess.CanManage(EventWithGroup(null), "user-9", true));

    [Fact]
    public void CanManage_ReturnsFalse_WhenUserIsCreatorOnly()
    {
        // C46 (1.5): creating the event alone no longer grants management.
        var ev = EventWithGroup(null);
        Assert.False(EventAccess.CanManage(ev, "creator-1", false));
    }

    [Fact]
    public void CanManage_ReturnsFalse_WhenUserIsPlainMember()
        => Assert.False(EventAccess.CanManage(EventWithGroup(GroupMemberRole.Member), "user-2", false));

    [Fact]
    public void CanManage_ReturnsFalse_WhenUserNotInGroup()
        => Assert.False(EventAccess.CanManage(EventWithGroup(GroupMemberRole.Admin, "user-3"), "user-2", false));

    [Fact]
    public void CanManage_ReturnsFalse_WhenEventIsNull()
        => Assert.False(EventAccess.CanManage(null, "user-1", true));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CanManage_ReturnsFalse_WhenUserIdIsEmpty(string? userId)
        => Assert.False(EventAccess.CanManage(EventWithGroup(GroupMemberRole.Admin), userId, false));

    [Fact]
    public void CanManage_ReturnsFalse_WhenGroupIsNull()
    {
        var ev = new Event { CreatedByUserId = "u", Group = null! };
        Assert.False(EventAccess.CanManage(ev, "u", false));
    }
}
