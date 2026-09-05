using FluentAssertions;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Application.Queries;
using Hris.Foundation.Notification.Application.Validators;
using Hris.Foundation.Notification.Domain;
using Xunit;

namespace Hris.Foundation.Notification.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>WorkflowEngineCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class NotificationCommandValidatorsTests
{
    [Fact]
    public void CreateNotificationTemplateCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new CreateNotificationTemplateCommandValidator();
        var valid = new CreateNotificationTemplateCommand(
            TestData.TenantId, "leave.approved", NotificationType.ApprovalResult, "en-US", "Subject", "Body", TestData.NewSupportedChannels());
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNewNotificationTemplateDraftVersionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTemplateId()
    {
        var validator = new CreateNewNotificationTemplateDraftVersionCommandValidator();
        var valid = new CreateNewNotificationTemplateDraftVersionCommand(Guid.NewGuid(), "en-US", "Subject", "Body", TestData.NewSupportedChannels());
        var invalid = valid with { NotificationTemplateId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishNotificationTemplateVersionCommandValidator_AcceptsAValidCommand_AndRejectsAZeroVersionNumber()
    {
        var validator = new PublishNotificationTemplateVersionCommandValidator();
        var valid = new PublishNotificationTemplateVersionCommand(Guid.NewGuid(), 1);
        var invalid = valid with { VersionNumber = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeprecateNotificationTemplateVersionCommandValidator_AcceptsAValidCommand_AndRejectsAZeroVersionNumber()
    {
        var validator = new DeprecateNotificationTemplateVersionCommandValidator();
        var valid = new DeprecateNotificationTemplateVersionCommand(Guid.NewGuid(), 1);
        var invalid = valid with { VersionNumber = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RequestNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyBody()
    {
        var validator = new RequestNotificationCommandValidator();
        var valid = new RequestNotificationCommand(
            TestData.TenantId, TestData.RecipientUserId, NotificationType.ApprovalResult, NotificationChannel.InApp, null, null, "Body");
        var invalid = valid with { Body = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void QueueNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new QueueNotificationCommandValidator();
        var valid = new QueueNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ScheduleNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new ScheduleNotificationCommandValidator();
        var valid = new ScheduleNotificationCommand(Guid.NewGuid(), TestData.NowUtc.AddHours(1));
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartProcessingNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new StartProcessingNotificationCommandValidator();
        var valid = new StartProcessingNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkNotificationSentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new MarkNotificationSentCommandValidator();
        var valid = new MarkNotificationSentCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkNotificationDeliveredCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new MarkNotificationDeliveredCommandValidator();
        var valid = new MarkNotificationDeliveredCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AcknowledgeNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new AcknowledgeNotificationCommandValidator();
        var valid = new AcknowledgeNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new FailNotificationCommandValidator();
        var valid = new FailNotificationCommand(Guid.NewGuid(), "Provider unavailable");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RetryNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new RetryNotificationCommandValidator();
        var valid = new RetryNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveNotificationToDeadLetterCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new MoveNotificationToDeadLetterCommandValidator();
        var valid = new MoveNotificationToDeadLetterCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExpireNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new ExpireNotificationCommandValidator();
        var valid = new ExpireNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SuppressNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNotificationId()
    {
        var validator = new SuppressNotificationCommandValidator();
        var valid = new SuppressNotificationCommand(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelNotificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new CancelNotificationCommandValidator();
        var valid = new CancelNotificationCommand(Guid.NewGuid(), "No longer needed");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkNotificationReadCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyActorUserId()
    {
        var validator = new MarkNotificationReadCommandValidator();
        var valid = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { ActorUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterNotificationPreferenceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyUserId()
    {
        var validator = new RegisterNotificationPreferenceCommandValidator();
        var valid = new RegisterNotificationPreferenceCommand(
            TestData.TenantId, TestData.RecipientUserId, "en-US", TestData.NewSupportedChannels(), null, null, false, false);
        var invalid = valid with { UserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateNotificationPreferenceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPreferenceId()
    {
        var validator = new UpdateNotificationPreferenceCommandValidator();
        var valid = new UpdateNotificationPreferenceCommand(Guid.NewGuid(), null, [], null, null, false, false);
        var invalid = valid with { NotificationPreferenceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetNotificationTemplateQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTemplateId()
    {
        var validator = new GetNotificationTemplateQueryValidator();
        var valid = new GetNotificationTemplateQuery(Guid.NewGuid());
        var invalid = valid with { NotificationTemplateId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListNotificationTemplatesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListNotificationTemplatesQueryValidator();
        var valid = new ListNotificationTemplatesQuery(TestData.TenantId);
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetNotificationQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyNotificationId()
    {
        var validator = new GetNotificationQueryValidator();
        var valid = new GetNotificationQuery(Guid.NewGuid());
        var invalid = valid with { NotificationId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetMyNotificationsQueryValidator_AcceptsAValidQuery_AndRejectsAZeroTake()
    {
        var validator = new GetMyNotificationsQueryValidator();
        var valid = new GetMyNotificationsQuery(TestData.RecipientUserId, TestData.TenantId, null, 0, 20);
        var invalid = valid with { Take = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetNotificationPreferenceQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyUserId()
    {
        var validator = new GetNotificationPreferenceQueryValidator();
        var valid = new GetNotificationPreferenceQuery(TestData.TenantId, TestData.RecipientUserId);
        var invalid = valid with { UserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
