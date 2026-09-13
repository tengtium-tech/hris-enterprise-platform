using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeaveRequest</c> commands. Overlap, statutory-
/// details, and lifecycle rules (LV-031, LV-034, LV-037) belong to the aggregate or its
/// handler; here we confirm the request is well-formed (application/validations.md).
/// </summary>
public sealed class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.DateRange).NotNull();
        RuleFor(c => c.SubmittedBy).NotEmpty();
    }
}

public sealed class ApproveLeaveRequestCommandValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveRequestId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.PaidDays).GreaterThanOrEqualTo(0);
    }
}

public sealed class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveRequestId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelLeaveRequestCommandValidator : AbstractValidator<CancelLeaveRequestCommand>
{
    public CancelLeaveRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveRequestId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class ApplyLeaveBalanceDeductionCommandValidator : AbstractValidator<ApplyLeaveBalanceDeductionCommand>
{
    public ApplyLeaveBalanceDeductionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.LeaveRequestId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}

public sealed class ApplyLeaveBalanceCompensationCommandValidator : AbstractValidator<ApplyLeaveBalanceCompensationCommand>
{
    public ApplyLeaveBalanceCompensationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.LeaveRequestId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}
