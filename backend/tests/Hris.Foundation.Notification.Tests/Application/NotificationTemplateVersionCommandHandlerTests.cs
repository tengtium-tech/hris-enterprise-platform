using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationTemplateVersionCommandHandlerTests
{
    private readonly INotificationTemplateRepository _repository = Substitute.For<INotificationTemplateRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task CreateNewDraftVersion_Succeeds_WhenTemplateExists()
    {
        var template = TestData.PublishedTemplate();
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        var handler = new CreateNewNotificationTemplateDraftVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new CreateNewNotificationTemplateDraftVersionCommand(template.Id.Value, "en-US", "Subject", "Body", TestData.NewSupportedChannels()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task CreateNewDraftVersion_Fails_WhenTemplateDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);
        var handler = new CreateNewNotificationTemplateDraftVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new CreateNewNotificationTemplateDraftVersionCommand(Guid.NewGuid(), "en-US", "Subject", "Body", TestData.NewSupportedChannels()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.TemplateNotFound);
    }

    [Fact]
    public async Task PublishVersion_Succeeds_WhenTemplateAndVersionExist()
    {
        var template = TestData.NewTemplate();
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        var handler = new PublishNotificationTemplateVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new PublishNotificationTemplateVersionCommand(template.Id.Value, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Versions[0].Status.Should().Be(NotificationTemplateVersionStatus.Published);
    }

    [Fact]
    public async Task PublishVersion_Fails_WhenTemplateDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);
        var handler = new PublishNotificationTemplateVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new PublishNotificationTemplateVersionCommand(Guid.NewGuid(), 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.TemplateNotFound);
    }

    [Fact]
    public async Task DeprecateVersion_Succeeds_WhenTemplateAndVersionExist()
    {
        var template = TestData.PublishedTemplate();
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        var handler = new DeprecateNotificationTemplateVersionCommandHandler(_repository);

        var result = await handler.Handle(new DeprecateNotificationTemplateVersionCommand(template.Id.Value, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Versions[0].Status.Should().Be(NotificationTemplateVersionStatus.Deprecated);
    }

    [Fact]
    public async Task DeprecateVersion_Fails_WhenTemplateDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);
        var handler = new DeprecateNotificationTemplateVersionCommandHandler(_repository);

        var result = await handler.Handle(new DeprecateNotificationTemplateVersionCommand(Guid.NewGuid(), 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.TemplateNotFound);
    }
}
