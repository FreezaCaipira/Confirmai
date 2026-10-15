using Confirmai.Services.Core;
using System.Reflection;

namespace Confirmai.Tests;

public class AuditEventsTests
{
    [Fact]
    public void UserRegistered_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.registered", AuditEvents.UserRegistered);
    }

    [Fact]
    public void UserLoginSuccess_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.login.success", AuditEvents.UserLoginSuccess);
    }

    [Fact]
    public void UserLoginFailed_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.login.failed", AuditEvents.UserLoginFailed);
    }

    [Fact]
    public void UserLockedOut_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.locked", AuditEvents.UserLockedOut);
    }

    [Fact]
    public void UserPasswordChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.password.changed", AuditEvents.UserPasswordChanged);
    }

    [Fact]
    public void UserPasswordReset_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.password.reset", AuditEvents.UserPasswordReset);
    }

    [Fact]
    public void UserRoleAssigned_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.role.assigned", AuditEvents.UserRoleAssigned);
    }

    [Fact]
    public void UserRoleRemoved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.role.removed", AuditEvents.UserRoleRemoved);
    }

    [Fact]
    public void UserDeleted_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("user.deleted", AuditEvents.UserDeleted);
    }

    [Fact]
    public void ProductCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("product.created", AuditEvents.ProductCreated);
    }

    [Fact]
    public void ProductUpdated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("product.updated", AuditEvents.ProductUpdated);
    }

    [Fact]
    public void ProductDeleted_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("product.deleted", AuditEvents.ProductDeleted);
    }

    [Fact]
    public void ProductArchived_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("product.archived", AuditEvents.ProductArchived);
    }

    [Fact]
    public void PaymentConfirmed_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.confirmed", AuditEvents.PaymentConfirmed);
    }

    [Fact]
    public void PaymentFailed_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.failed", AuditEvents.PaymentFailed);
    }

    [Fact]
    public void PaymentRefunded_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.refunded", AuditEvents.PaymentRefunded);
    }

    [Fact]
    public void PaymentStatusChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.status.changed", AuditEvents.PaymentStatusChanged);
    }

    [Fact]
    public void PaymentReconciliationSweep_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.reconciliation.sweep", AuditEvents.PaymentReconciliationSweep);
    }

    [Fact]
    public void PaymentReconciliationPanelStale_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.reconciliation.panel.stale", AuditEvents.PaymentReconciliationPanelStale);
    }

    [Fact]
    public void AdminSecurityPolicyChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.security_policy.changed", AuditEvents.AdminSecurityPolicyChanged);
    }

    [Fact]
    public void AdminSettingChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.setting.changed", AuditEvents.AdminSettingChanged);
    }

    [Fact]
    public void AdminAuditViewed_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.audit.viewed", AuditEvents.AdminAuditViewed);
    }

    [Fact]
    public void EventCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.created", AuditEvents.EventCreated);
    }

    [Fact]
    public void EventUpdated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.updated", AuditEvents.EventUpdated);
    }

    [Fact]
    public void EventCancelled_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.cancelled", AuditEvents.EventCancelled);
    }

    [Fact]
    public void EventCapacityChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.capacity.changed", AuditEvents.EventCapacityChanged);
    }

    [Fact]
    public void EventScoreRegistered_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.score.registered", AuditEvents.EventScoreRegistered);
    }

    [Fact]
    public void EventPostMatchVoteCast_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.vote.cast", AuditEvents.EventPostMatchVoteCast);
    }

    [Fact]
    public void EventConfirmationAdded_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.added", AuditEvents.EventConfirmationAdded);
    }

    [Fact]
    public void EventConfirmationRemoved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.removed", AuditEvents.EventConfirmationRemoved);
    }

    [Fact]
    public void EventConfirmationPaidManual_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.paid.manual", AuditEvents.EventConfirmationPaidManual);
    }

    [Fact]
    public void EventConfirmationUnpaidManual_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.unpaid.manual", AuditEvents.EventConfirmationUnpaidManual);
    }

    [Fact]
    public void EventConfirmationTableChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.table.changed", AuditEvents.EventConfirmationTableChanged);
    }

    [Fact]
    public void EventConfirmationRestamped_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.confirmation.restamped", AuditEvents.EventConfirmationRestamped);
    }

    [Fact]
    public void EventWaitlistJoined_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.waitlist.joined", AuditEvents.EventWaitlistJoined);
    }

    [Fact]
    public void EventWaitlistPromoted_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.waitlist.promoted", AuditEvents.EventWaitlistPromoted);
    }

    [Fact]
    public void EventWaitlistRemoved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("event.waitlist.removed", AuditEvents.EventWaitlistRemoved);
    }

    [Fact]
    public void GroupCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.created", AuditEvents.GroupCreated);
    }

    [Fact]
    public void GroupUpdated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.updated", AuditEvents.GroupUpdated);
    }

    [Fact]
    public void GroupMemberAdded_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.member.added", AuditEvents.GroupMemberAdded);
    }

    [Fact]
    public void GroupMemberRemoved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.member.removed", AuditEvents.GroupMemberRemoved);
    }

    [Fact]
    public void GroupMemberRoleChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.member.role.changed", AuditEvents.GroupMemberRoleChanged);
    }

    [Fact]
    public void GroupMemberRoleChangeDenied_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.member.role.change.denied", AuditEvents.GroupMemberRoleChangeDenied);
    }

    [Fact]
    public void GroupFeatureToggled_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.feature.toggled", AuditEvents.GroupFeatureToggled);
    }

    [Fact]
    public void GroupPixReceiverChanged_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.pix.receiver.changed", AuditEvents.GroupPixReceiverChanged);
    }

    [Fact]
    public void GroupInviteOpened_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.invite.opened", AuditEvents.GroupInviteOpened);
    }

    [Fact]
    public void GroupMemberJoined_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("group.member.joined", AuditEvents.GroupMemberJoined);
    }

    [Fact]
    public void PageView_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("page.view", AuditEvents.PageView);
    }

    [Fact]
    public void DelinquencyNotified_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("delinquency.notified", AuditEvents.DelinquencyNotified);
    }

    /// <summary>
    /// C42-B guardian: every constant in <see cref="AuditEvents"/> must be
    /// referenced by production source code. A constant nobody emits is dead
    /// weight that suggests audit coverage where none exists — remove it or
    /// wire it. Marketplace-era constants were already pruned in C42-B.
    /// </summary>
    [Fact]
    public void EveryConstant_IsReferencedByProductionCode()
    {
        var root = RepoRoot();
        var source = CollectProductionSource(root);

        var dead = typeof(AuditEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Where(f => !source.Contains($"AuditEvents.{f.Name}", StringComparison.Ordinal))
            .Select(f => f.Name)
            .ToList();

        Assert.True(dead.Count == 0,
            "AuditEvents constants not referenced by production code: " + string.Join(", ", dead));
    }

    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }

    private static string CollectProductionSource(DirectoryInfo root)
    {
        var dirs = new[] { "Areas", "Pages", "Services", "Shared", "Components", "Controllers", "Data" };
        var files = dirs
            .Select(d => Path.Combine(root.FullName, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        var program = Path.Combine(root.FullName, "Program.cs");
        var all = files.Append(program).Where(File.Exists);
        return string.Join("\n", all.Select(File.ReadAllText));
    }
}
