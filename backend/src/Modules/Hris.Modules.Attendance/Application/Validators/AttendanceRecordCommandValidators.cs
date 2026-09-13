using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>AttendanceRecord</c> commands only — every business
/// rule (AT-001, AT-012, AT-030, AT-031, AT-032) stays in the aggregate, per
/// application/validations.md. The <see cref="ValidationBehavior{TRequest,TResponse}"/> runs these
/// before the handler, so an empty required identifier is rejected without touching the aggregate.
/// </summary>
public sealed class CaptureTimeEventCommandValidator : AbstractValidator<CaptureTimeEventCommand>
{
    public CaptureTimeEventCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
    }
}

public sealed class RunCalculationCommandValidator : AbstractValidator<RunCalculationCommand>
{
    public RunCalculationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.Trigger).NotEmpty();
    }
}

public sealed class SubmitAttendanceForApprovalCommandValidator : AbstractValidator<SubmitAttendanceForApprovalCommand>
{
    public SubmitAttendanceForApprovalCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ApproveAttendanceCommandValidator : AbstractValidator<ApproveAttendanceCommand>
{
    public ApproveAttendanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class RejectAttendanceCommandValidator : AbstractValidator<RejectAttendanceCommand>
{
    public RejectAttendanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class FinalizeAttendanceCommandValidator : AbstractValidator<FinalizeAttendanceCommand>
{
    public FinalizeAttendanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ReopenAttendanceCommandValidator : AbstractValidator<ReopenAttendanceCommand>
{
    public ReopenAttendanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.AuthorizationReference).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class MarkRecordAdjustmentSubmittedCommandValidator : AbstractValidator<MarkRecordAdjustmentSubmittedCommand>
{
    public MarkRecordAdjustmentSubmittedCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
    }
}

public sealed class ResolveRecordAdjustmentCommandValidator : AbstractValidator<ResolveRecordAdjustmentCommand>
{
    public ResolveRecordAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
    }
}
