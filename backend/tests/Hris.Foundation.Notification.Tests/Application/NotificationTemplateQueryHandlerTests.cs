using FluentAssertions;
using Hris.Foundation.Notification.Application.Queries;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationTemplateQueryHandlerTests
{
    private readonly INotificationTemplateRepository _repository = Substitute.For<INotificationTemplateRepository>();

    [Fact]
    public async Task GetNotificationTemplateQuery_Succeeds_AndReturnsEveryFieldMapped()
    {
        var template = TestData.PublishedTemplate();
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        var handler = new GetNotificationTemplateQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationTemplateQuery(template.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.NotificationTemplateId.Should().Be(template.Id.Value);
        dto.TenantId.Should().Be(template.TenantId);
        dto.TemplateKey.Should().Be(template.TemplateKey);
        dto.NotificationType.Should().Be(template.NotificationType.ToString());
        dto.CreatedAtUtc.Should().Be(template.CreatedAtUtc);
        dto.Versions.Should().ContainSingle();

        var versionDto = dto.Versions[0];
        var version = template.Versions[0];
        versionDto.NotificationTemplateVersionId.Should().Be(version.Id.Value);
        versionDto.VersionNumber.Should().Be(version.VersionNumber);
        versionDto.Locale.Should().Be(version.Locale);
        versionDto.Subject.Should().Be(version.Subject);
        versionDto.Body.Should().Be(version.Body);
        versionDto.SupportedChannels.Should().HaveSameCount(version.SupportedChannels);
        versionDto.Status.Should().Be(version.Status.ToString());
        versionDto.CreatedAtUtc.Should().Be(version.CreatedAtUtc);
        versionDto.PublishedAtUtc.Should().Be(version.PublishedAtUtc);
    }

    [Fact]
    public async Task GetNotificationTemplateQuery_Fails_WhenTemplateDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);
        var handler = new GetNotificationTemplateQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationTemplateQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.TemplateNotFound);
    }

    [Fact]
    public async Task ListNotificationTemplatesQuery_Succeeds_AndReturnsMappedDtos()
    {
        var templates = new List<NotificationTemplate> { TestData.NewTemplate() };
        _repository.ListByTenantAsync(TestData.TenantId, Arg.Any<CancellationToken>()).Returns(templates);
        var handler = new ListNotificationTemplatesQueryHandler(_repository);

        var result = await handler.Handle(new ListNotificationTemplatesQuery(TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}
