using FluentValidation;
using Hris.Modules.Workflow.Application.Commands;

namespace Hris.Modules.Workflow.Application.Validators;

public sealed class CreateApprovalDelegationCommandValidator : AbstractValidator<CreateApprovalDelegationCommand>
{
    public CreateApprovalDelegationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DelegatorUserAccountId).NotEmpty();
        RuleFor(c => c.DelegateUserAccountId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
        RuleFor(c => c.ActingUser).NotEmpty();

        // WR-040: both ends of the period are required. A command shape carrying two
        // non-nullable dates already guarantees presence, so what is left to check
        // here is ordering; the aggregate re-checks it through DateRange regardless.
        RuleFor(c => c.PeriodEnd).GreaterThanOrEqualTo(c => c.PeriodStart);
    }
}

public sealed class ActivateApprovalDelegationCommandValidator : AbstractValidator<ActivateApprovalDelegationCommand>
{
    public ActivateApprovalDelegationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DelegationId).NotEmpty();
    }
}

public sealed class ExpireApprovalDelegationCommandValidator : AbstractValidator<ExpireApprovalDelegationCommand>
{
    public ExpireApprovalDelegationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DelegationId).NotEmpty();
    }
}

public sealed class RevokeApprovalDelegationCommandValidator : AbstractValidator<RevokeApprovalDelegationCommand>
{
    public RevokeApprovalDelegationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DelegationId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}
