using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

public sealed class StartEmployeeOnboardingCommandValidator : AbstractValidator<StartEmployeeOnboardingCommand>
{
    public StartEmployeeOnboardingCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateEmployeeCommandValidator : AbstractValidator<ActivateEmployeeCommand>
{
    public ActivateEmployeeCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class StartEmployeeOffboardingCommandValidator : AbstractValidator<StartEmployeeOffboardingCommand>
{
    public StartEmployeeOffboardingCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SeparateEmployeeCommandValidator : AbstractValidator<SeparateEmployeeCommand>
{
    public SeparateEmployeeCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RetireEmployeeCommandValidator : AbstractValidator<RetireEmployeeCommand>
{
    public RetireEmployeeCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RehireEmployeeCommandValidator : AbstractValidator<RehireEmployeeCommand>
{
    public RehireEmployeeCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RecordEmployeeDeceasedCommandValidator : AbstractValidator<RecordEmployeeDeceasedCommand>
{
    public RecordEmployeeDeceasedCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
