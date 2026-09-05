using FluentAssertions;
using Hris.Foundation.Notification.Application.Queries;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationPreferenceQueryHandlerTests
{
    private readonly INotificationPreferenceRepository _repository = Substitute.For<INotificationPreferenceRepository>();

    [Fact]
    public async Task Handle_Succeeds_AndReturnsEveryFieldMapped()
    {
        var preference = TestData.NewPreference();
        _repository.GetByUserAsync(TestData.TenantId, TestData.RecipientUserId, Arg.Any<CancellationToken>()).Returns(preference);
        var handler = new GetNotificationPreferenceQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationPreferenceQuery(TestData.TenantId, TestData.RecipientUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.NotificationPreferenceId.Should().Be(preference.Id.Value);
        dto.TenantId.Should().Be(preference.TenantId);
        dto.UserId.Should().Be(preference.UserId);
        dto.PreferredLanguage.Should().Be(preference.PreferredLanguage);
        dto.PreferredChannels.Should().HaveSameCount(preference.PreferredChannels);
        dto.QuietHoursStart.Should().Be(preference.QuietHoursStart);
        dto.QuietHoursEnd.Should().Be(preference.QuietHoursEnd);
        dto.DigestMode.Should().Be(preference.DigestMode);
        dto.OptedOut.Should().Be(preference.OptedOut);
        dto.CreatedAtUtc.Should().Be(preference.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_Fails_WhenPreferenceDoesNotExist()
    {
        _repository.GetByUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((NotificationPreference?)null);
        var handler = new GetNotificationPreferenceQueryHandler(_repository);

        var result = await handler.Handle(new GetNotificationPreferenceQuery(TestData.TenantId, TestData.RecipientUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.PreferenceNotFound);
    }
}
