using FluentAssertions;
using Hris.Foundation.Notification.Application.Queries;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationQueryHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task GetNotificationQuery_Succeeds_AndReturnsEveryFieldMapped()
    {
        var notification = TestData.ProcessingNotification();
        notification.Fail("Provider unavailable", TestData.NowUtc);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new GetNotificationQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationQuery(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.NotificationId.Should().Be(notification.Id.Value);
        dto.TenantId.Should().Be(notification.TenantId);
        dto.RecipientUserId.Should().Be(notification.RecipientUserId);
        dto.NotificationType.Should().Be(notification.NotificationType.ToString());
        dto.Channel.Should().Be(notification.Channel.ToString());
        dto.TemplateKey.Should().Be(notification.TemplateKey);
        dto.Subject.Should().Be(notification.Subject);
        dto.Body.Should().Be(notification.Body);
        dto.Status.Should().Be(notification.Status.ToString());
        dto.RequestedAtUtc.Should().Be(notification.RequestedAtUtc);
        dto.ScheduledForUtc.Should().Be(notification.ScheduledForUtc);
        dto.SentAtUtc.Should().Be(notification.SentAtUtc);
        dto.DeliveredAtUtc.Should().Be(notification.DeliveredAtUtc);
        dto.ReadAtUtc.Should().Be(notification.ReadAtUtc);
        dto.AcknowledgedAtUtc.Should().Be(notification.AcknowledgedAtUtc);
        dto.FailureReason.Should().Be(notification.FailureReason);
        dto.RetryCount.Should().Be(notification.RetryCount);
        dto.CancellationReason.Should().Be(notification.CancellationReason);
    }

    [Fact]
    public async Task GetNotificationQuery_Fails_WhenNotificationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>()).Returns((NotificationEntity?)null);
        var handler = new GetNotificationQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotificationNotFound);
    }

    [Fact]
    public async Task GetMyNotificationsQuery_Succeeds_AndReturnsItemsWithTotalAndUnreadCounts()
    {
        var items = new List<NotificationEntity> { TestData.DeliveredNotification() };
        _repository.ListInAppForRecipientAsync(TestData.RecipientUserId, TestData.TenantId, null, 0, 20, Arg.Any<CancellationToken>())
            .Returns((items, 1, 1));
        var handler = new GetMyNotificationsQueryHandler(_repository);

        var result = await handler.Handle(
            new GetMyNotificationsQuery(TestData.RecipientUserId, TestData.TenantId, null, 0, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.TotalCount.Should().Be(1);
        result.Value.UnreadCount.Should().Be(1);
    }
}
