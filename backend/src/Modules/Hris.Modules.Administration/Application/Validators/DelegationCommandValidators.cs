using FluentValidation;
using Hris.Modules.Administration.Application.Commands;

namespace Hris.Modules.Administration.Application.Validators;

public sealed class CreateDelegationCommandValidator : AbstractValidator<CreateDelegationCommand>
{
    public CreateDelegationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DelegatorUserAccountId).NotEmpty();
        RuleFor(c => c.DelegateUserAccountId).NotEmpty();
        RuleFor(c => c.DelegatedAuthority).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.ActingAdministrator).NotEmpty();
    }
}

public sealed class ActivateDelegationCommandValidator : AbstractValidator<ActivateDelegationCommand>
{
    public ActivateDelegationCommandValidator()
    {
        RuleFor(c => c.DelegationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ExpireDelegationCommandValidator : AbstractValidator<ExpireDelegationCommand>
{
    public ExpireDelegationCommandValidator()
    {
        RuleFor(c => c.DelegationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RevokeDelegationCommandValidator : AbstractValidator<RevokeDelegationCommand>
{
    public RevokeDelegationCommandValidator()
    {
        RuleFor(c => c.DelegationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.RevokedBy).NotEmpty();
    }
}
