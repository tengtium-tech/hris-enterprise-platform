using FluentAssertions;
using Hris.Foundation.Notification.Domain;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Domain;

public sealed class NotificationPreferenceTests
{
    [Fact]
    public void Register_Succeeds_AndRaisesNoEvent()
    {
        var result = NotificationPreference.Register(
            TestData.TenantId, TestData.RecipientUserId, "en-US", TestData.NewSupportedChannels(),
            TimeSpan.FromHours(22), TimeSpan.FromHours(7), digestMode: false, optedOut: false, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.UserId.Should().Be(TestData.RecipientUserId);
        result.Value.PreferredLanguage.Should().Be("en-US");
        result.Value.QuietHoursStart.Should().Be(TimeSpan.FromHours(22));
        result.Value.QuietHoursEnd.Should().Be(TimeSpan.FromHours(7));
        result.Value.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Register_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => NotificationPreference.Register(
            Guid.Empty, TestData.RecipientUserId, null, [], null, null, false, false, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_Throws_WhenUserIdIsEmpty()
    {
        var act = () => NotificationPreference.Register(
            TestData.TenantId, Guid.Empty, null, [], null, null, false, false, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Succeeds_AndReplacesEveryField()
    {
        var preference = TestData.NewPreference();

        var result = preference.Update("fr-FR", [NotificationChannel.Sms], TimeSpan.FromHours(21), TimeSpan.FromHours(6), true, true);

        result.IsSuccess.Should().BeTrue();
        preference.PreferredLanguage.Should().Be("fr-FR");
        preference.PreferredChannels.Should().ContainSingle().Which.Should().Be(NotificationChannel.Sms);
        preference.QuietHoursStart.Should().Be(TimeSpan.FromHours(21));
        preference.DigestMode.Should().BeTrue();
        preference.OptedOut.Should().BeTrue();
    }
}
