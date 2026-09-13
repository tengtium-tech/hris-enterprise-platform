using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>AttendancePolicy</c> commands. Versioning and assignment
/// overlap rules (AT-002, AT-011) belong to the aggregate; here we confirm the request carries the
/// identifiers and configuration it must (application/validations.md).
/// </summary>
public sealed class DefineAttendancePolicyCommandValidator : AbstractValidator<DefineAttendancePolicyCommand>
{
    public DefineAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Configuration).NotNull();
        RuleFor(c => c.CreatedBy).NotEmpty();
    }
}

public sealed class PublishAttendancePolicyCommandValidator : AbstractValidator<PublishAttendancePolicyCommand>
{
    public PublishAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendancePolicyId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ReviseAttendancePolicyCommandValidator : AbstractValidator<ReviseAttendancePolicyCommand>
{
    public ReviseAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendancePolicyId).NotEmpty();
        RuleFor(c => c.Configuration).NotNull();
        RuleFor(c => c.ChangedBy).NotEmpty();
    }
}

public sealed class AssignAttendancePolicyCommandValidator : AbstractValidator<AssignAttendancePolicyCommand>
{
    public AssignAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendancePolicyId).NotEmpty();
        RuleFor(c => c.PolicyAssignmentId).NotEmpty();
        RuleFor(c => c.ScopeTargetId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.AssignedBy).NotEmpty();
    }
}

public sealed class UnassignAttendancePolicyCommandValidator : AbstractValidator<UnassignAttendancePolicyCommand>
{
    public UnassignAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendancePolicyId).NotEmpty();
        RuleFor(c => c.PolicyAssignmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class RetireAttendancePolicyCommandValidator : AbstractValidator<RetireAttendancePolicyCommand>
{
    public RetireAttendancePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendancePolicyId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
