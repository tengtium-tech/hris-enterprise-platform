using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeaveEncashment</c> commands. Entitlement (LV-074),
/// commutability/sufficiency (LV-070), and the duplicate-pending rule (LV-072) live in the
/// handler and the aggregate respectively; here we only confirm the request is well-formed
/// (application/validations.md).
/// </summary>
public sealed class SubmitLeaveEncashmentCommandValidator : AbstractValidator<SubmitLeaveEncashmentCommand>
{
    public SubmitLeaveEncashmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.RequestedAmount).GreaterThan(0);
        RuleFor(c => c.SubmittedBy).NotEmpty();
    }
}

public sealed class ApproveLeaveEncashmentCommandValidator : AbstractValidator<ApproveLeaveEncashmentCommand>
{
    public ApproveLeaveEncashmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveEncashmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class RejectLeaveEncashmentCommandValidator : AbstractValidator<RejectLeaveEncashmentCommand>
{
    public RejectLeaveEncashmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveEncashmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelLeaveEncashmentCommandValidator : AbstractValidator<CancelLeaveEncashmentCommand>
{
    public CancelLeaveEncashmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveEncashmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class ApplyLeaveEncashmentCommandValidator : AbstractValidator<ApplyLeaveEncashmentCommand>
{
    public ApplyLeaveEncashmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.LeaveEncashmentId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}
