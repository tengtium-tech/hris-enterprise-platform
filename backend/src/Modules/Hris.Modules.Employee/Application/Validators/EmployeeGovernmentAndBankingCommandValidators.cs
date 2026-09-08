using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

public sealed class UpdateEmployeeGovernmentInformationCommandValidator : AbstractValidator<UpdateEmployeeGovernmentInformationCommand>
{
    public UpdateEmployeeGovernmentInformationCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class UpdateEmployeeBankInformationCommandValidator : AbstractValidator<UpdateEmployeeBankInformationCommand>
{
    public UpdateEmployeeBankInformationCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
