using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationLifecycleCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task Queue_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.NewNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new QueueNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new QueueNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Queued);
    }

    [Fact]
    public async Task Queue_Fails_WhenNotificationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>()).Returns((NotificationEntity?)null);
        var handler = new QueueNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new QueueNotificationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotificationNotFound);
    }

    [Fact]
    public async Task Schedule_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.QueuedNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new ScheduleNotificationCommandHandler(_repository);

        var result = await handler.Handle(
            new ScheduleNotificationCommand(notification.Id.Value, TestData.NowUtc.AddHours(1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Scheduled);
    }

    [Fact]
    public async Task StartProcessing_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.QueuedNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new StartProcessingNotificationCommandHandler(_repository);

        var result = await handler.Handle(new StartProcessingNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Processing);
    }

    [Fact]
    public async Task MarkSent_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.ProcessingNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new MarkNotificationSentCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new MarkNotificationSentCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task MarkDelivered_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.SentNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new MarkNotificationDeliveredCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new MarkNotificationDeliveredCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Delivered);
    }

    [Fact]
    public async Task Acknowledge_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new AcknowledgeNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new AcknowledgeNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Acknowledged);
    }

    [Fact]
    public async Task Fail_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.ProcessingNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new FailNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new FailNotificationCommand(notification.Id.Value, "Provider unavailable"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public async Task Retry_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.ProcessingNotification();
        notification.Fail("Provider unavailable", TestData.NowUtc);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new RetryNotificationCommandHandler(_repository);

        var result = await handler.Handle(new RetryNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Queued);
        notification.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task MoveToDeadLetter_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.ProcessingNotification();
        notification.Fail("Provider unavailable", TestData.NowUtc);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new MoveNotificationToDeadLetterCommandHandler(_repository);

        var result = await handler.Handle(new MoveNotificationToDeadLetterCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.DeadLetter);
    }

    [Fact]
    public async Task Expire_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.QueuedNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new ExpireNotificationCommandHandler(_repository);

        var result = await handler.Handle(new ExpireNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Expired);
    }

    [Fact]
    public async Task Suppress_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.QueuedNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new SuppressNotificationCommandHandler(_repository);

        var result = await handler.Handle(new SuppressNotificationCommand(notification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Suppressed);
    }

    [Fact]
    public async Task Cancel_Succeeds_WhenNotificationExists()
    {
        var notification = TestData.NewNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        var handler = new CancelNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelNotificationCommand(notification.Id.Value, "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_Fails_WhenNotificationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>()).Returns((NotificationEntity?)null);
        var handler = new CancelNotificationCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelNotificationCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotificationNotFound);
    }
}
