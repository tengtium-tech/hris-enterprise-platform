using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

public sealed class AddEmergencyContactCommandValidator : AbstractValidator<AddEmergencyContactCommand>
{
    public AddEmergencyContactCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Relationship).NotEmpty();
        RuleFor(c => c.Phone).NotEmpty();
    }
}

public sealed class UpdateEmergencyContactCommandValidator : AbstractValidator<UpdateEmergencyContactCommand>
{
    public UpdateEmergencyContactCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmergencyContactId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Relationship).NotEmpty();
        RuleFor(c => c.Phone).NotEmpty();
    }
}

public sealed class RemoveEmergencyContactCommandValidator : AbstractValidator<RemoveEmergencyContactCommand>
{
    public RemoveEmergencyContactCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmergencyContactId).NotEmpty();
    }
}
