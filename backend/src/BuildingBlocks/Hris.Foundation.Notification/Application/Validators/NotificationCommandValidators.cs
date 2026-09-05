using FluentValidation;
using Hris.Foundation.Notification.Application.Commands;
using Hris.Foundation.Notification.Application.Queries;

namespace Hris.Foundation.Notification.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the Domain
/// layer's own factory/transition methods already enforce (template content shape,
/// recipient-ownership gating, lifecycle-state gating) -- the identical separation
/// every other framework's own validators file states for its own set.
/// </summary>
public sealed class CreateNotificationTemplateCommandValidator : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.TemplateKey).NotEmpty();
        RuleFor(c => c.Locale).NotEmpty();
        RuleFor(c => c.Body).NotEmpty();
        RuleFor(c => c.SupportedChannels).NotEmpty();
    }
}

public sealed class CreateNewNotificationTemplateDraftVersionCommandValidator
    : AbstractValidator<CreateNewNotificationTemplateDraftVersionCommand>
{
    public CreateNewNotificationTemplateDraftVersionCommandValidator()
    {
        RuleFor(c => c.NotificationTemplateId).NotEmpty();
        RuleFor(c => c.Locale).NotEmpty();
        RuleFor(c => c.Body).NotEmpty();
        RuleFor(c => c.SupportedChannels).NotEmpty();
    }
}

public sealed class PublishNotificationTemplateVersionCommandValidator : AbstractValidator<PublishNotificationTemplateVersionCommand>
{
    public PublishNotificationTemplateVersionCommandValidator()
    {
        RuleFor(c => c.NotificationTemplateId).NotEmpty();
        RuleFor(c => c.VersionNumber).GreaterThan(0);
    }
}

public sealed class DeprecateNotificationTemplateVersionCommandValidator : AbstractValidator<DeprecateNotificationTemplateVersionCommand>
{
    public DeprecateNotificationTemplateVersionCommandValidator()
    {
        RuleFor(c => c.NotificationTemplateId).NotEmpty();
        RuleFor(c => c.VersionNumber).GreaterThan(0);
    }
}

public sealed class RequestNotificationCommandValidator : AbstractValidator<RequestNotificationCommand>
{
    public RequestNotificationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.RecipientUserId).NotEmpty();
        RuleFor(c => c.Body).NotEmpty();
    }
}

public sealed class QueueNotificationCommandValidator : AbstractValidator<QueueNotificationCommand>
{
    public QueueNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class ScheduleNotificationCommandValidator : AbstractValidator<ScheduleNotificationCommand>
{
    public ScheduleNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class StartProcessingNotificationCommandValidator : AbstractValidator<StartProcessingNotificationCommand>
{
    public StartProcessingNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class MarkNotificationSentCommandValidator : AbstractValidator<MarkNotificationSentCommand>
{
    public MarkNotificationSentCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class MarkNotificationDeliveredCommandValidator : AbstractValidator<MarkNotificationDeliveredCommand>
{
    public MarkNotificationDeliveredCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class AcknowledgeNotificationCommandValidator : AbstractValidator<AcknowledgeNotificationCommand>
{
    public AcknowledgeNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class FailNotificationCommandValidator : AbstractValidator<FailNotificationCommand>
{
    public FailNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class RetryNotificationCommandValidator : AbstractValidator<RetryNotificationCommand>
{
    public RetryNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class MoveNotificationToDeadLetterCommandValidator : AbstractValidator<MoveNotificationToDeadLetterCommand>
{
    public MoveNotificationToDeadLetterCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class ExpireNotificationCommandValidator : AbstractValidator<ExpireNotificationCommand>
{
    public ExpireNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class SuppressNotificationCommandValidator : AbstractValidator<SuppressNotificationCommand>
{
    public SuppressNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}

public sealed class CancelNotificationCommandValidator : AbstractValidator<CancelNotificationCommand>
{
    public CancelNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(c => c.NotificationId).NotEmpty();
        RuleFor(c => c.ActorUserId).NotEmpty();
    }
}

public sealed class RegisterNotificationPreferenceCommandValidator : AbstractValidator<RegisterNotificationPreferenceCommand>
{
    public RegisterNotificationPreferenceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(c => c.NotificationPreferenceId).NotEmpty();
    }
}

public sealed class GetNotificationTemplateQueryValidator : AbstractValidator<GetNotificationTemplateQuery>
{
    public GetNotificationTemplateQueryValidator()
    {
        RuleFor(q => q.NotificationTemplateId).NotEmpty();
    }
}

public sealed class ListNotificationTemplatesQueryValidator : AbstractValidator<ListNotificationTemplatesQuery>
{
    public ListNotificationTemplatesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetNotificationQueryValidator : AbstractValidator<GetNotificationQuery>
{
    public GetNotificationQueryValidator()
    {
        RuleFor(q => q.NotificationId).NotEmpty();
    }
}

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(q => q.RecipientUserId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Skip).GreaterThanOrEqualTo(0);
        RuleFor(q => q.Take).GreaterThan(0);
    }
}

public sealed class GetNotificationPreferenceQueryValidator : AbstractValidator<GetNotificationPreferenceQuery>
{
    public GetNotificationPreferenceQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.UserId).NotEmpty();
    }
}
