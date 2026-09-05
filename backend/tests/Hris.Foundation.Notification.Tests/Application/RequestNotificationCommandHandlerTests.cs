using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;
using NotificationEntity = Hris.Foundation.Notification.Domain.Notification;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class RequestNotificationCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly RequestNotificationCommandHandler _handler;

    public RequestNotificationCommandHandlerTests()
    {
        _handler = new RequestNotificationCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static RequestNotificationCommand ValidCommand() => new(
        TestData.TenantId, TestData.RecipientUserId, NotificationType.ApprovalResult, NotificationChannel.InApp,
        "leave.approved", "Subject", "Body");

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewNotification()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenBodyIsMissing_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand() with { Body = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.BodyRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>());
    }
}
