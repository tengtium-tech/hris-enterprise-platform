using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

public sealed class NotificationPreferenceCommandHandlerTests
{
    private readonly INotificationPreferenceRepository _repository = Substitute.For<INotificationPreferenceRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task Register_Succeeds_AndPersistsTheNewPreference()
    {
        var handler = new RegisterNotificationPreferenceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new RegisterNotificationPreferenceCommand(
                TestData.TenantId, TestData.RecipientUserId, "en-US", TestData.NewSupportedChannels(),
                TimeSpan.FromHours(22), TimeSpan.FromHours(7), false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_Fails_WhenPreferenceAlreadyExists_WithoutCallingAddAsync()
    {
        _repository.ExistsByUserAsync(TestData.TenantId, TestData.RecipientUserId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterNotificationPreferenceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new RegisterNotificationPreferenceCommand(
                TestData.TenantId, TestData.RecipientUserId, "en-US", TestData.NewSupportedChannels(), null, null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.PreferenceAlreadyExists);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Succeeds_WhenPreferenceExists()
    {
        var preference = TestData.NewPreference();
        _repository.GetByIdAsync(preference.Id, Arg.Any<CancellationToken>()).Returns(preference);
        var handler = new UpdateNotificationPreferenceCommandHandler(_repository);

        var result = await handler.Handle(
            new UpdateNotificationPreferenceCommand(
                preference.Id.Value, "fr-FR", [NotificationChannel.Sms], null, null, true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        preference.PreferredLanguage.Should().Be("fr-FR");
        preference.DigestMode.Should().BeTrue();
    }

    [Fact]
    public async Task Update_Fails_WhenPreferenceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NotificationPreferenceId>(), Arg.Any<CancellationToken>()).Returns((NotificationPreference?)null);
        var handler = new UpdateNotificationPreferenceCommandHandler(_repository);

        var result = await handler.Handle(
            new UpdateNotificationPreferenceCommand(Guid.NewGuid(), null, [], null, null, false, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.PreferenceNotFound);
    }
}
