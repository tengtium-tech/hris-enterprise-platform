using FluentValidation;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Application.Queries;

namespace Hris.Foundation.Scheduling.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (expression/time-zone
/// shape, lifecycle-state gating) -- the identical separation every other framework's
/// own validators file states for its own set.
/// </summary>
public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Expression).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
        RuleFor(c => c.TaskType).NotEmpty();
    }
}

public sealed class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
        RuleFor(c => c.Expression).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
        RuleFor(c => c.TaskType).NotEmpty();
    }
}

public sealed class ValidateScheduleCommandValidator : AbstractValidator<ValidateScheduleCommand>
{
    public ValidateScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class ApproveScheduleCommandValidator : AbstractValidator<ApproveScheduleCommand>
{
    public ApproveScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class ActivateScheduleCommandValidator : AbstractValidator<ActivateScheduleCommand>
{
    public ActivateScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class PauseScheduleCommandValidator : AbstractValidator<PauseScheduleCommand>
{
    public PauseScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class ResumeScheduleCommandValidator : AbstractValidator<ResumeScheduleCommand>
{
    public ResumeScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class RetireScheduleCommandValidator : AbstractValidator<RetireScheduleCommand>
{
    public RetireScheduleCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
    }
}

public sealed class TriggerScheduleExecutionCommandValidator : AbstractValidator<TriggerScheduleExecutionCommand>
{
    public TriggerScheduleExecutionCommandValidator()
    {
        RuleFor(c => c.ScheduleId).NotEmpty();
        RuleFor(c => c.RetryCount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CompleteScheduleExecutionCommandValidator : AbstractValidator<CompleteScheduleExecutionCommand>
{
    public CompleteScheduleExecutionCommandValidator()
    {
        RuleFor(c => c.ScheduleExecutionId).NotEmpty();
    }
}

public sealed class FailScheduleExecutionCommandValidator : AbstractValidator<FailScheduleExecutionCommand>
{
    public FailScheduleExecutionCommandValidator()
    {
        RuleFor(c => c.ScheduleExecutionId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class GetScheduleQueryValidator : AbstractValidator<GetScheduleQuery>
{
    public GetScheduleQueryValidator()
    {
        RuleFor(q => q.ScheduleId).NotEmpty();
    }
}

public sealed class ListScheduleExecutionHistoryQueryValidator : AbstractValidator<ListScheduleExecutionHistoryQuery>
{
    public ListScheduleExecutionHistoryQueryValidator()
    {
        RuleFor(q => q.ScheduleId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
