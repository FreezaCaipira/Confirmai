using Confirmai.Enums;

namespace Confirmai.Tests;

public class GroupMemberRoleTests
{
    [Fact]
    public void Member_HasCorrectValue()
    {
        // Assert
        Assert.Equal(0, (int)GroupMemberRole.Member);
    }

    [Fact]
    public void Admin_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)GroupMemberRole.Admin);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[] { GroupMemberRole.Member, GroupMemberRole.Admin };

        // Assert
        Assert.Equal(2, values.Distinct().Count());
    }

    [Fact]
    public void Member_IsNotAdmin()
    {
        // Assert
        Assert.NotEqual(GroupMemberRole.Member, GroupMemberRole.Admin);
    }

    [Fact]
    public void Admin_IsNotMember()
    {
        // Assert
        Assert.NotEqual(GroupMemberRole.Admin, GroupMemberRole.Member);
    }
}
