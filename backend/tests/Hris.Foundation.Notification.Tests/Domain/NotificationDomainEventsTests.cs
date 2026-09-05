using FluentAssertions;
using Hris.Foundation.Notification.Domain;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Domain;

public sealed class NotificationDomainEventsTests
{
    [Fact]
    public void Request_RaisesNotificationRequested_CarryingTheExpectedFields()
    {
        var notification = TestData.NewNotification();

        var raised = notification.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<NotificationRequested>().Subject;

        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.NotificationId.Should().Be(notification.Id);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.RecipientUserId.Should().Be(TestData.RecipientUserId);
        raised.NotificationType.Should().Be(NotificationType.ApprovalResult);
        raised.Channel.Should().Be(NotificationChannel.InApp);
    }

    [Fact]
    public void Queue_RaisesNotificationQueued_CarryingTheExpectedFields()
    {
        var notification = TestData.NewNotification();

        notification.Queue(TestData.NowUtc);

        var raised = notification.DomainEvents.OfType<NotificationQueued>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.NotificationId.Should().Be(notification.Id);
    }

    [Fact]
    public void MarkRead_RaisesNotificationRead_CarryingTheActorAsRecipient()
    {
        var notification = TestData.DeliveredNotification();

        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        var raised = notification.DomainEvents.OfType<NotificationRead>().Should().ContainSingle().Subject;
        raised.NotificationId.Should().Be(notification.Id);
        raised.RecipientUserId.Should().Be(TestData.RecipientUserId);
    }

    [Fact]
    public void Fail_RaisesNotificationFailed_CarryingTheReason()
    {
        var notification = TestData.ProcessingNotification();

        notification.Fail("Provider unavailable", TestData.NowUtc);

        var raised = notification.DomainEvents.OfType<NotificationFailed>().Should().ContainSingle().Subject;
        raised.NotificationId.Should().Be(notification.Id);
        raised.Reason.Should().Be("Provider unavailable");
    }

    [Fact]
    public void Cancel_RaisesNotificationCancelled_CarryingTheReason()
    {
        var notification = TestData.NewNotification();

        notification.Cancel("No longer needed", TestData.NowUtc);

        var raised = notification.DomainEvents.OfType<NotificationCancelled>().Should().ContainSingle().Subject;
        raised.NotificationId.Should().Be(notification.Id);
        raised.Reason.Should().Be("No longer needed");
    }

    [Fact]
    public void PublishVersion_RaisesNotificationTemplateUpdated_CarryingTheVersionNumber()
    {
        var template = TestData.NewTemplate();

        template.PublishVersion(1, TestData.NowUtc);

        var raised = template.DomainEvents.OfType<NotificationTemplateUpdated>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.NotificationTemplateId.Should().Be(template.Id);
        raised.VersionNumber.Should().Be(1);
    }
}
