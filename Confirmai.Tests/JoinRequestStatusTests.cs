using Confirmai.Enums;

namespace Confirmai.Tests;

public class JoinRequestStatusTests
{
    [Fact]
    public void Pending_HasCorrectValue()
    {
        // Assert
        Assert.Equal(0, (int)JoinRequestStatus.Pending);
    }

    [Fact]
    public void Approved_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)JoinRequestStatus.Approved);
    }

    [Fact]
    public void Rejected_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)JoinRequestStatus.Rejected);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[] { JoinRequestStatus.Pending, JoinRequestStatus.Approved, JoinRequestStatus.Rejected };

        // Assert
        Assert.Equal(3, values.Distinct().Count());
    }

    [Fact]
    public void Pending_IsNotApproved()
    {
        // Assert
        Assert.NotEqual(JoinRequestStatus.Pending, JoinRequestStatus.Approved);
    }

    [Fact]
    public void Pending_IsNotRejected()
    {
        // Assert
        Assert.NotEqual(JoinRequestStatus.Pending, JoinRequestStatus.Rejected);
    }

    [Fact]
    public void Approved_IsNotRejected()
    {
        // Assert
        Assert.NotEqual(JoinRequestStatus.Approved, JoinRequestStatus.Rejected);
    }
}
