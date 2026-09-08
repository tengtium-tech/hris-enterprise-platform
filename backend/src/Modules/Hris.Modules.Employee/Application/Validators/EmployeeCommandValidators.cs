using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (name shape,
/// per-tenant Employee Number uniqueness, lifecycle-state gating) -- the identical
/// separation <c>EmploymentCommandValidators</c> already states for its own set.
/// </summary>
public sealed class RegisterEmployeeCommandValidator : AbstractValidator<RegisterEmployeeCommand>
{
    public RegisterEmployeeCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.DateOfBirth).NotEqual(default(DateOnly));
    }
}

public sealed class UpdateEmployeePersonalInformationCommandValidator : AbstractValidator<UpdateEmployeePersonalInformationCommand>
{
    public UpdateEmployeePersonalInformationCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.DateOfBirth).NotEqual(default(DateOnly));
    }
}

public sealed class UpdateEmployeePhotoCommandValidator : AbstractValidator<UpdateEmployeePhotoCommand>
{
    public UpdateEmployeePhotoCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
