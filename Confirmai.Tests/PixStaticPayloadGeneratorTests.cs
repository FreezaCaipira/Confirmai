using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

public class PixStaticPayloadGeneratorTests
{
    private static GroupMember MakeMember(string userId, string? pixKey, GroupMemberRole role, DateTime createdAt)
        => new()
        {
            UserId = userId,
            User = new ApplicationUser { Id = userId, PixKey = pixKey },
            Role = role,
            CreatedAt = createdAt
        };

    // ── GetGroupAdminPixKey ──────────────────────────────────────────────

    [Fact]
    public void GetGroupAdminPixKey_NoReceiverConfigured_ReturnsFirstAdminWithPixKey()
    {
        var group = new Group
        {
            PixReceiverUserId = null,
            Members =
            {
                MakeMember("u1", null, GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
                MakeMember("u2", "admin2@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 2)),
                MakeMember("u3", "admin3@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 3)),
            }
        };

        Assert.Equal("admin2@pix.com", PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_ReceiverConfigured_ReturnsChosenReceiverPixKey()
    {
        var group = new Group
        {
            PixReceiverUserId = "u3",
            Members =
            {
                MakeMember("u1", "admin1@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
                MakeMember("u2", "admin2@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 2)),
                MakeMember("u3", "admin3@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 3)),
            }
        };

        Assert.Equal("admin3@pix.com", PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_ReceiverConfiguredButNoPixKey_FallsBackToFirstAdminWithKey()
    {
        var group = new Group
        {
            PixReceiverUserId = "u1",
            Members =
            {
                MakeMember("u1", null, GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
                MakeMember("u2", "admin2@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 2)),
            }
        };

        Assert.Equal("admin2@pix.com", PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_NoAdminsWithPixKey_ReturnsNull()
    {
        var group = new Group
        {
            PixReceiverUserId = null,
            Members =
            {
                MakeMember("u1", null, GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
                MakeMember("u2", null, GroupMemberRole.Member, new DateTime(2025, 1, 2)),
            }
        };

        Assert.Null(PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_EmptyMembers_ReturnsNull()
    {
        var group = new Group { PixReceiverUserId = null };

        Assert.Null(PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_MembersWithPixKeyButNotAdmin_ReturnsNull()
    {
        var group = new Group
        {
            PixReceiverUserId = null,
            Members =
            {
                MakeMember("u1", "member1@pix.com", GroupMemberRole.Member, new DateTime(2025, 1, 1)),
            }
        };

        Assert.Null(PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_ReceiverConfiguredButNotMember_FallsBackToFirstAdmin()
    {
        var group = new Group
        {
            PixReceiverUserId = "nonexistent",
            Members =
            {
                MakeMember("u1", "admin1@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
            }
        };

        Assert.Equal("admin1@pix.com", PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    [Fact]
    public void GetGroupAdminPixKey_FallbackRespectsCreatedAtOrder()
    {
        var group = new Group
        {
            PixReceiverUserId = null,
            Members =
            {
                MakeMember("u3", "late@pix.com", GroupMemberRole.Admin, new DateTime(2025, 6, 1)),
                MakeMember("u1", "early@pix.com", GroupMemberRole.Admin, new DateTime(2025, 1, 1)),
                MakeMember("u2", "mid@pix.com", GroupMemberRole.Admin, new DateTime(2025, 3, 1)),
            }
        };

        Assert.Equal("early@pix.com", PixStaticPayloadGenerator.GetGroupAdminPixKey(group));
    }

    // ── Build ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ValidInputs_ReturnsValidBrCode()
    {
        var result = PixStaticPayloadGenerator.Build("user@pix.com", "Meu Grupo", "S\u00E3o Paulo", 25.50m);

        Assert.StartsWith("000201", result);
        Assert.Contains("br.gov.bcb.pix", result);
        Assert.Contains("user@pix.com", result);
        Assert.Contains("Meu Grupo", result);
        Assert.Contains("25.50", result);
        Assert.Contains("5802BR", result);
        var crc = result[^4..];
        Assert.Matches(@"^[0-9A-F]{4}$", crc);
    }

    [Fact]
    public void Build_NullCity_UsesBrasilAsDefault()
    {
        var result = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", null, 10m);

        Assert.Contains("Brasil", result);
    }

    [Fact]
    public void Build_EmptyCity_UsesBrasilAsDefault()
    {
        var result = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "", 10m);

        Assert.Contains("Brasil", result);
    }

    [Fact]
    public void Build_LongGroupName_TruncatesTo25Chars()
    {
        var longName = new string('A', 30);
        var result = PixStaticPayloadGenerator.Build("key@pix.com", longName, "City", 10m);

        Assert.Contains(new string('A', 25), result);
        Assert.DoesNotContain(new string('A', 26), result);
    }

    [Fact]
    public void Build_LongCity_TruncatesTo15Chars()
    {
        var longCity = new string('B', 20);
        var result = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", longCity, 10m);

        Assert.Contains(new string('B', 15), result);
        Assert.DoesNotContain(new string('B', 16), result);
    }

    [Fact]
    public void Build_ZeroAmount_FormatsAs000()
    {
        var result = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "City", 0m);

        Assert.Contains("0.00", result);
    }

    [Fact]
    public void Build_SameInputs_ProducesSameOutput()
    {
        var result1 = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "City", 15.99m);
        var result2 = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "City", 15.99m);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Build_DifferentAmounts_ProduceDifferentOutputs()
    {
        var result1 = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "City", 10m);
        var result2 = PixStaticPayloadGenerator.Build("key@pix.com", "Grupo", "City", 20m);

        Assert.NotEqual(result1, result2);
    }
}
