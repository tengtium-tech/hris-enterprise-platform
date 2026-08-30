using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-NTF-001 through CTR-NTF-004, docs/09-testing/critical-test-requirements.md §10.
/// Meaningful once docs/03-foundation/notification-framework.md has a real
/// implementation (Sprint 4 or later, per IMPLEMENTATION-PLAN.md).
/// </summary>
public class NotificationDeliveryTests
{
    [Fact(Skip = "Not yet implemented. CTR-NTF-001 — Business Operations Do Not Wait for Delivery.")]
    public void CTR_NTF_001_BusinessOperationsDoNotWaitForDelivery()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-NTF-002 — Notification Failure Does Not Roll Back Business Actions.")]
    public void CTR_NTF_002_NotificationFailureDoesNotRollBackBusinessActions()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-NTF-003 — Committed Actions Do Not Lose Their Notifications.")]
    public void CTR_NTF_003_CommittedActionsDoNotLoseTheirNotifications()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-NTF-004 — Provider Outage Does Not Block Core Operation.")]
    public void CTR_NTF_004_ProviderOutageDoesNotBlockCoreOperation()
    {
    }
}
