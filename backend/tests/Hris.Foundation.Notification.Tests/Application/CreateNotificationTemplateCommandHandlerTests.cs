using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class CreateNotificationTemplateCommandHandlerTests
{
    private readonly INotificationTemplateRepository _repository = Substitute.For<INotificationTemplateRepository>();
    private readonly CreateNotificationTemplateCommandHandler _handler;

    public CreateNotificationTemplateCommandHandlerTests()
    {
        _handler = new CreateNotificationTemplateCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static CreateNotificationTemplateCommand ValidCommand() => new(
        TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject", "Body {{EmployeeName}}",
        TestData.NewSupportedChannels());

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewTemplate()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTemplateKeyAlreadyExists_WithoutCallingAddAsync()
    {
        _repository.ExistsByTemplateKeyAsync(TestData.TenantId, "leave.approved", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.DuplicateTemplateKey);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenBodyIsMissing()
    {
        var result = await _handler.Handle(ValidCommand() with { Body = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.BodyRequired);
    }
}
