using FluentAssertions;
using Hris.Foundation.Notification.Domain;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Domain;

public sealed class NotificationTemplateTests
{
    [Fact]
    public void Create_Succeeds_WithFirstDraftVersion_AndRaisesNoEvent()
    {
        var result = NotificationTemplate.Create(
            TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject",
            "Body {{EmployeeName}}", TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.TemplateKey.Should().Be("leave.approved");
        result.Value.Versions.Should().ContainSingle();
        result.Value.Versions[0].Status.Should().Be(NotificationTemplateVersionStatus.Draft);
        result.Value.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => NotificationTemplate.Create(
            Guid.Empty, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject", "Body",
            TestData.NewSupportedChannels(), TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenTemplateKeyIsMissing(string? templateKey)
    {
        var result = NotificationTemplate.Create(
            TestData.TenantId, templateKey, NotificationType.ApprovalResult, "en-US", "Subject", "Body",
            TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.TemplateKeyRequired);
    }

    [Fact]
    public void Create_Fails_WhenLocaleIsMissing()
    {
        var result = NotificationTemplate.Create(
            TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, string.Empty, "Subject", "Body",
            TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.LocaleRequired);
    }

    [Fact]
    public void Create_Fails_WhenBodyIsMissing()
    {
        var result = NotificationTemplate.Create(
            TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject", null,
            TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.BodyRequired);
    }

    [Fact]
    public void Create_Fails_WhenSupportedChannelsAreEmpty()
    {
        var result = NotificationTemplate.Create(
            TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject", "Body", [], TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.SupportedChannelsRequired);
    }

    [Fact]
    public void CreateNewDraftVersion_Succeeds_WhenNoDraftExists()
    {
        var template = TestData.PublishedTemplate();

        var result = template.CreateNewDraftVersion("en-US", "New subject", "New body", TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(2);
        template.Versions.Should().HaveCount(2);
    }

    [Fact]
    public void CreateNewDraftVersion_Fails_WhenADraftAlreadyExists()
    {
        var template = TestData.NewTemplate();

        var result = template.CreateNewDraftVersion("en-US", "Subject", "Body", TestData.NewSupportedChannels(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.DraftAlreadyExists);
    }

    [Fact]
    public void PublishVersion_Succeeds_AndRaisesNotificationTemplateUpdated_AndDeprecatesThePreviousVersion()
    {
        var template = TestData.PublishedTemplate();
        template.CreateNewDraftVersion("en-US", "Subject", "Body", TestData.NewSupportedChannels(), TestData.NowUtc);

        var result = template.PublishVersion(2, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        template.Versions.Single(v => v.VersionNumber == 2).Status.Should().Be(NotificationTemplateVersionStatus.Published);
        template.Versions.Single(v => v.VersionNumber == 1).Status.Should().Be(NotificationTemplateVersionStatus.Deprecated);
        template.DomainEvents.OfType<NotificationTemplateUpdated>().Should().HaveCount(2, "publishing v1 earlier and v2 here each raise their own event")
            .And.Contain(e => e.VersionNumber == 2);
    }

    [Fact]
    public void PublishVersion_Fails_WhenVersionNumberDoesNotExist()
    {
        var template = TestData.NewTemplate();

        var result = template.PublishVersion(99, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.VersionNotFound);
    }

    [Fact]
    public void PublishVersion_Fails_WhenVersionIsNotDraft()
    {
        var template = TestData.PublishedTemplate();

        var result = template.PublishVersion(1, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.InvalidVersionLifecycleTransition);
    }

    [Fact]
    public void DeprecateVersion_Succeeds_FromPublished()
    {
        var template = TestData.PublishedTemplate();

        var result = template.DeprecateVersion(1);

        result.IsSuccess.Should().BeTrue();
        template.Versions[0].Status.Should().Be(NotificationTemplateVersionStatus.Deprecated);
    }

    [Fact]
    public void DeprecateVersion_Fails_WhenVersionNumberDoesNotExist()
    {
        var template = TestData.NewTemplate();

        var result = template.DeprecateVersion(99);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.VersionNotFound);
    }

    [Fact]
    public void GetPublishedVersion_ReturnsNull_WhenNoVersionIsPublished()
    {
        TestData.NewTemplate().GetPublishedVersion().Should().BeNull();
    }

    [Fact]
    public void GetPublishedVersion_ReturnsThePublishedVersion()
    {
        var template = TestData.PublishedTemplate();

        template.GetPublishedVersion().Should().NotBeNull();
        template.GetPublishedVersion()!.VersionNumber.Should().Be(1);
    }
}
