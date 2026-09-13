using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeaveAdjustment</c> commands. Entitlement (LV-050)
/// and the duplicate-pending / snapshot rules (LV-053, LV-054) live in the handler and the
/// aggregate respectively; here we only confirm the request is well-formed
/// (application/validations.md).
/// </summary>
public sealed class SubmitLeaveAdjustmentCommandValidator : AbstractValidator<SubmitLeaveAdjustmentCommand>
{
    public SubmitLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.RequestedAmount).NotEqual(0);
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.SubmittedBy).NotEmpty();
    }
}

public sealed class ReviewLeaveAdjustmentCommandValidator : AbstractValidator<ReviewLeaveAdjustmentCommand>
{
    public ReviewLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
        RuleFor(c => c.ReviewerId).NotEmpty();
        RuleFor(c => c.Notes).NotEmpty();
    }
}

public sealed class ApproveLeaveAdjustmentCommandValidator : AbstractValidator<ApproveLeaveAdjustmentCommand>
{
    public ApproveLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class RejectLeaveAdjustmentCommandValidator : AbstractValidator<RejectLeaveAdjustmentCommand>
{
    public RejectLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelLeaveAdjustmentCommandValidator : AbstractValidator<CancelLeaveAdjustmentCommand>
{
    public CancelLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class ApplyLeaveAdjustmentCommandValidator : AbstractValidator<ApplyLeaveAdjustmentCommand>
{
    public ApplyLeaveAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
        RuleFor(c => c.Amount).NotEqual(0);
    }
}

public sealed class MarkLeaveAdjustmentAppliedCommandValidator : AbstractValidator<MarkLeaveAdjustmentAppliedCommand>
{
    public MarkLeaveAdjustmentAppliedCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveAdjustmentId).NotEmpty();
    }
}
