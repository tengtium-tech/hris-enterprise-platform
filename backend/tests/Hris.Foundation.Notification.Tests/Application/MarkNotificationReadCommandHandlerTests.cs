using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class MarkNotificationReadCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly MarkNotificationReadCommandHandler _handler;

    public MarkNotificationReadCommandHandlerTests()
    {
        _handler = new MarkNotificationReadCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenActorIsTheRecipient()
    {
        var notification = TestData.DeliveredNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(
            new MarkNotificationReadCommand(notification.Id.Value, TestData.RecipientUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Read);
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenAlreadyRead()
    {
        var notification = TestData.DeliveredNotification();
        notification.MarkRead(TestData.RecipientUserId, TestData.NowUtc);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(
            new MarkNotificationReadCommand(notification.Id.Value, TestData.RecipientUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenActorIsNotTheRecipient()
    {
        var notification = TestData.DeliveredNotification();
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(
            new MarkNotificationReadCommand(notification.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotAuthorizedForThisNotification);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotificationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>()).Returns((NotificationEntity?)null);

        var result = await _handler.Handle(new MarkNotificationReadCommand(Guid.NewGuid(), TestData.RecipientUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotificationNotFound);
    }
}
