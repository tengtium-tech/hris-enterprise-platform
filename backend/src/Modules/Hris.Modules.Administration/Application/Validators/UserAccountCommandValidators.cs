using FluentValidation;
using Hris.Modules.Administration.Application.Commands;

namespace Hris.Modules.Administration.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (account-type
/// construction rules, AR-001/002/003, last-administrator guard) -- the identical
/// separation every prior module's own command validators already state.
/// </summary>
public sealed class ProvisionUserAccountCommandValidator : AbstractValidator<ProvisionUserAccountCommand>
{
    public ProvisionUserAccountCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ProvisionedBy).NotEmpty();
    }
}

public sealed class ActivateUserAccountCommandValidator : AbstractValidator<ActivateUserAccountCommand>
{
    public ActivateUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SuspendUserAccountCommandValidator : AbstractValidator<SuspendUserAccountCommand>
{
    public SuspendUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SuspendedBy).NotEmpty();
    }
}

public sealed class ReinstateUserAccountCommandValidator : AbstractValidator<ReinstateUserAccountCommand>
{
    public ReinstateUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeprovisionUserAccountCommandValidator : AbstractValidator<DeprovisionUserAccountCommand>
{
    public DeprovisionUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DeprovisionedBy).NotEmpty();
    }
}
