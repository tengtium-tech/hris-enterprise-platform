using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>AttendanceAdjustment</c> commands. The duplicate-pending
/// and snapshot rules (AT-020, AT-021) live in the aggregate; here we only confirm the request is
/// well-formed and references resolvable identifiers (application/validations.md).
/// </summary>
public sealed class SubmitAttendanceAdjustmentCommandValidator : AbstractValidator<SubmitAttendanceAdjustmentCommand>
{
    public SubmitAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.Field).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.SubmittedBy).NotEmpty();
    }
}

public sealed class ReviewAttendanceAdjustmentCommandValidator : AbstractValidator<ReviewAttendanceAdjustmentCommand>
{
    public ReviewAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
        RuleFor(c => c.ReviewerId).NotEmpty();
        RuleFor(c => c.Notes).NotEmpty();
    }
}

public sealed class ApproveAttendanceAdjustmentCommandValidator : AbstractValidator<ApproveAttendanceAdjustmentCommand>
{
    public ApproveAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class RejectAttendanceAdjustmentCommandValidator : AbstractValidator<RejectAttendanceAdjustmentCommand>
{
    public RejectAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelAttendanceAdjustmentCommandValidator : AbstractValidator<CancelAttendanceAdjustmentCommand>
{
    public CancelAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class ApplyAttendanceAdjustmentCommandValidator : AbstractValidator<ApplyAttendanceAdjustmentCommand>
{
    public ApplyAttendanceAdjustmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceRecordId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
        RuleFor(c => c.Field).NotEmpty();
    }
}

public sealed class MarkAdjustmentAppliedCommandValidator : AbstractValidator<MarkAdjustmentAppliedCommand>
{
    public MarkAdjustmentAppliedCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceAdjustmentId).NotEmpty();
    }
}
