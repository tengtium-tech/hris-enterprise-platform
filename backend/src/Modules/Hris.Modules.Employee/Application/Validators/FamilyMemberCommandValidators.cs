using FluentValidation;
using Hris.Modules.Employee.Application.Commands;

namespace Hris.Modules.Employee.Application.Validators;

public sealed class AddFamilyMemberCommandValidator : AbstractValidator<AddFamilyMemberCommand>
{
    public AddFamilyMemberCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class UpdateFamilyMemberCommandValidator : AbstractValidator<UpdateFamilyMemberCommand>
{
    public UpdateFamilyMemberCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FamilyMemberId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class RemoveFamilyMemberCommandValidator : AbstractValidator<RemoveFamilyMemberCommand>
{
    public RemoveFamilyMemberCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FamilyMemberId).NotEmpty();
    }
}
