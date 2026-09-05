using FluentAssertions;
using Hris.Foundation.Notification.Domain;
using Xunit;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void Request_Succeeds_InCreated_AndRaisesNotificationRequested()
    {
        var result = NotificationEntity.Request(
            TestData.TenantId, TestData.RecipientUserId, NotificationType.ApprovalResult, NotificationChannel.InApp,
            "leave.approved", "Subject", "Body", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(NotificationStatus.Created);
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.RecipientUserId.Should().Be(TestData.RecipientUserId);
        result.Value.DomainEvents.OfType<NotificationRequested>().Should().ContainSingle();
    }

    [Fact]
    public void Request_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => NotificationEntity.Request(
            Guid.Empty, TestData.RecipientUserId, NotificationType.ApprovalResult, NotificationChannel.InApp, null, null, "Body", TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Request_Throws_WhenRecipientUserIdIsEmpty()
    {
        var act = () => NotificationEntity.Request(
            TestData.TenantId, Guid.Empty, NotificationType.ApprovalResult, NotificationChannel.InApp, null, null, "Body", TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Request_Fails_WhenBodyIsMissing(string? body)
    {
        var result = NotificationEntity.Request(
            TestData.TenantId, TestData.RecipientUserId, NotificationType.ApprovalResult, NotificationChannel.InApp, null, null, body, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.BodyRequired);
    }

    [Fact]
    public void Queue_Succeeds_FromCreated_AndRaisesNotificationQueued()
    {
        var notification = TestData.NewNotification();

        var result = notification.Queue(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Queued);
        notification.DomainEvents.OfType<NotificationQueued>().Should().ContainSingle();
    }

    [Fact]
    public void Queue_Fails_WhenNotCreated()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.Queue(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Schedule_Succeeds_FromQueued_AndRaisesNoEvent()
    {
        var notification = TestData.QueuedNotification();
        var scheduledForUtc = TestData.NowUtc.AddHours(2);

        var result = notification.Schedule(scheduledForUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Scheduled);
        notification.ScheduledForUtc.Should().Be(scheduledForUtc);
        notification.DomainEvents.OfType<NotificationQueued>().Should().ContainSingle("Schedule itself raises no additional event");
    }

    [Fact]
    public void Schedule_Fails_WhenNotQueued()
    {
        var notification = TestData.NewNotification();

        var result = notification.Schedule(TestData.NowUtc.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void StartProcessing_Succeeds_FromQueued()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.StartProcessing();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Processing);
    }

    [Fact]
    public void StartProcessing_Succeeds_FromScheduled()
    {
        var notification = TestData.QueuedNotification();
        notification.Schedule(TestData.NowUtc.AddHours(1));

        var result = notification.StartProcessing();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Processing);
    }

    [Fact]
    public void StartProcessing_Fails_WhenNotQueuedOrScheduled()
    {
        var notification = TestData.NewNotification();

        var result = notification.StartProcessing();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void MarkSent_Succeeds_FromProcessing_AndRaisesNotificationSent()
    {
        var notification = TestData.ProcessingNotification();

        var result = notification.MarkSent(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAtUtc.Should().Be(TestData.NowUtc);
        notification.DomainEvents.OfType<NotificationSent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkSent_Fails_WhenNotProcessing()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.MarkSent(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void MarkDelivered_Succeeds_FromSent_AndRaisesNotificationDelivered()
    {
        var notification = TestData.SentNotification();

        var result = notification.MarkDelivered(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Delivered);
        notification.DeliveredAtUtc.Should().Be(TestData.NowUtc);
        notification.DomainEvents.OfType<NotificationDelivered>().Should().ContainSingle();
    }

    [Fact]
    public void MarkDelivered_Fails_WhenNotSent()
    {
        var notification = TestData.ProcessingNotification();

        var result = notification.MarkDelivered(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void MarkRead_Succeeds_FromDelivered_AndRaisesNotificationRead()
    {
        var notification = TestData.DeliveredNotification();

        var result = notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Read);
        notification.ReadAtUtc.Should().Be(TestData.NowUtc);
        notification.DomainEvents.OfType<NotificationRead>().Should().ContainSingle();
    }

    [Fact]
    public void MarkRead_IsIdempotent_WhenAlreadyRead()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        var result = notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        notification.DomainEvents.OfType<NotificationRead>().Should().ContainSingle("marking an already-read notification must not re-raise the event");
    }

    [Fact]
    public void MarkRead_Fails_WhenActorIsNotTheRecipient()
    {
        var notification = TestData.DeliveredNotification();

        var result = notification.MarkRead(Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotAuthorizedForThisNotification);
    }

    [Fact]
    public void MarkRead_Fails_WhenNotDelivered()
    {
        var notification = TestData.SentNotification();

        var result = notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Acknowledge_Succeeds_FromRead()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        var result = notification.Acknowledge(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Acknowledged);
    }

    [Fact]
    public void Acknowledge_Fails_WhenNotRead()
    {
        var notification = TestData.DeliveredNotification();

        var result = notification.Acknowledge(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Fail_Succeeds_FromNonTerminalState_AndRaisesNotificationFailed()
    {
        var notification = TestData.ProcessingNotification();

        var result = notification.Fail("Provider unavailable", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailureReason.Should().Be("Provider unavailable");
        notification.DomainEvents.OfType<NotificationFailed>().Should().ContainSingle();
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsMissing()
    {
        var notification = TestData.ProcessingNotification();

        var result = notification.Fail(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.FailureReasonRequired);
    }

    [Fact]
    public void Fail_Fails_WhenAlreadyTerminal()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);
        notification.Acknowledge(TestData.NowUtc);

        var result = notification.Fail("reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void RetryAfterFailure_Succeeds_FromFailed_AndIncrementsRetryCount()
    {
        var notification = TestData.ProcessingNotification();
        notification.Fail("Provider unavailable", TestData.NowUtc);

        var result = notification.RetryAfterFailure();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Queued);
        notification.RetryCount.Should().Be(1);
    }

    [Fact]
    public void RetryAfterFailure_Fails_WhenNotFailed()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.RetryAfterFailure();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void MoveToDeadLetter_Succeeds_FromFailed()
    {
        var notification = TestData.ProcessingNotification();
        notification.Fail("Provider unavailable", TestData.NowUtc);

        var result = notification.MoveToDeadLetter();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.DeadLetter);
    }

    [Fact]
    public void MoveToDeadLetter_Fails_WhenNotFailed()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.MoveToDeadLetter();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Expire_Succeeds_FromQueued()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.Expire();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Expired);
    }

    [Fact]
    public void Expire_Succeeds_FromScheduled()
    {
        var notification = TestData.QueuedNotification();
        notification.Schedule(TestData.NowUtc.AddHours(1));

        var result = notification.Expire();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Expired);
    }

    [Fact]
    public void Expire_Fails_WhenNotQueuedOrScheduled()
    {
        var notification = TestData.NewNotification();

        var result = notification.Expire();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Suppress_Succeeds_FromNonTerminalState()
    {
        var notification = TestData.QueuedNotification();

        var result = notification.Suppress();

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Suppressed);
    }

    [Fact]
    public void Suppress_Fails_WhenAlreadyTerminal()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        var result = notification.Suppress();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }

    [Fact]
    public void Cancel_Succeeds_FromNonTerminalState_AndRaisesNotificationCancelled()
    {
        var notification = TestData.NewNotification();

        var result = notification.Cancel("No longer needed", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Cancelled);
        notification.CancellationReason.Should().Be("No longer needed");
        notification.DomainEvents.OfType<NotificationCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_Fails_WhenReasonIsMissing()
    {
        var notification = TestData.NewNotification();

        var result = notification.Cancel(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.CancellationReasonRequired);
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyTerminal()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);

        var result = notification.Cancel("reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidNotificationLifecycleTransition);
    }
}
