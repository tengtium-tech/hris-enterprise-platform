using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>BiometricEnrollment</c> commands. Consent and
/// revocation-completion rules (AT-052, AT-053) belong to the aggregate; here we only confirm the
/// request names the employee and enrollment it must (application/validations.md).
/// </summary>
public sealed class EnrollBiometricCommandValidator : AbstractValidator<EnrollBiometricCommand>
{
    public EnrollBiometricCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ActivateBiometricEnrollmentCommandValidator : AbstractValidator<ActivateBiometricEnrollmentCommand>
{
    public ActivateBiometricEnrollmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BiometricEnrollmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class RevokeBiometricEnrollmentCommandValidator : AbstractValidator<RevokeBiometricEnrollmentCommand>
{
    public RevokeBiometricEnrollmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BiometricEnrollmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
