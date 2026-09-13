using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>OvertimeRequest</c> commands. The payable / duplicate
/// rules (AT-040, AT-041, AT-043) are the aggregate's responsibility; here we only confirm a
/// well-formed request (application/validations.md).
/// </summary>
public sealed class SubmitOvertimeRequestCommandValidator : AbstractValidator<SubmitOvertimeRequestCommand>
{
    public SubmitOvertimeRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Justification).NotEmpty();
        RuleFor(c => c.SubmittedBy).NotEmpty();
        RuleFor(c => c.EstimatedHours).GreaterThanOrEqualTo(0);
    }
}

public sealed class ApproveOvertimeRequestCommandValidator : AbstractValidator<ApproveOvertimeRequestCommand>
{
    public ApproveOvertimeRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OvertimeRequestId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class RejectOvertimeRequestCommandValidator : AbstractValidator<RejectOvertimeRequestCommand>
{
    public RejectOvertimeRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OvertimeRequestId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelOvertimeRequestCommandValidator : AbstractValidator<CancelOvertimeRequestCommand>
{
    public CancelOvertimeRequestCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OvertimeRequestId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
