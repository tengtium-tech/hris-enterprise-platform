using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

public sealed class UpdateEmployeeContactInformationCommandValidator : AbstractValidator<UpdateEmployeeContactInformationCommand>
{
    public UpdateEmployeeContactInformationCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
