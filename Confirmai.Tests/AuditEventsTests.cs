using Confirmai.Services.Core;

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
    public void ItemOfferCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("itemoffer.created", AuditEvents.ItemOfferCreated);
    }

    [Fact]
    public void ItemOfferCancelled_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("itemoffer.cancelled", AuditEvents.ItemOfferCancelled);
    }

    [Fact]
    public void ItemOfferSold_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("itemoffer.sold", AuditEvents.ItemOfferSold);
    }

    [Fact]
    public void ServerRequested_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.requested", AuditEvents.ServerRequested);
    }

    [Fact]
    public void ServerRequestApproved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.request.approved", AuditEvents.ServerRequestApproved);
    }

    [Fact]
    public void ServerRequestRejected_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.request.rejected", AuditEvents.ServerRequestRejected);
    }

    [Fact]
    public void ServerCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.created", AuditEvents.ServerCreated);
    }

    [Fact]
    public void ServerUpdated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.updated", AuditEvents.ServerUpdated);
    }

    [Fact]
    public void ServerDeleted_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.deleted", AuditEvents.ServerDeleted);
    }

    [Fact]
    public void ServerMemberAdded_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.member.added", AuditEvents.ServerMemberAdded);
    }

    [Fact]
    public void ServerMemberRemoved_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.member.removed", AuditEvents.ServerMemberRemoved);
    }

    [Fact]
    public void ServerMemberUpdated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.member.updated", AuditEvents.ServerMemberUpdated);
    }

    [Fact]
    public void ServerApiKeyIssued_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.apikey.issued", AuditEvents.ServerApiKeyIssued);
    }

    [Fact]
    public void ServerApiKeyRevoked_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("server.apikey.revoked", AuditEvents.ServerApiKeyRevoked);
    }

    [Fact]
    public void PaymentInvoiceCreated_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.invoice.created", AuditEvents.PaymentInvoiceCreated);
    }

    [Fact]
    public void PaymentReceived_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.received", AuditEvents.PaymentReceived);
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
    public void PaymentReplayed_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.replayed", AuditEvents.PaymentReplayed);
    }

    [Fact]
    public void PaymentInvalid_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.invalid", AuditEvents.PaymentInvalid);
    }

    [Fact]
    public void PaymentRefused_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("payment.refused", AuditEvents.PaymentRefused);
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
    public void DelinquencyNotified_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("delinquency.notified", AuditEvents.DelinquencyNotified);
    }

    [Fact]
    public void WebhookReceived_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("webhook.received", AuditEvents.WebhookReceived);
    }

    [Fact]
    public void WebhookUnauthorized_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("webhook.unauthorized", AuditEvents.WebhookUnauthorized);
    }

    [Fact]
    public void WebhookOversize_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("webhook.oversize", AuditEvents.WebhookOversize);
    }
}
