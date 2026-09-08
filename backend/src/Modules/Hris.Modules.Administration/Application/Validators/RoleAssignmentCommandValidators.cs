using FluentValidation;
using Hris.Modules.Administration.Application.Commands;

namespace Hris.Modules.Administration.Application.Validators;

public sealed class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.GrantedBy).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.RoleAssignmentId).NotEmpty();
        RuleFor(c => c.RevokedBy).NotEmpty();
    }
}

public sealed class ExpireRoleAssignmentCommandValidator : AbstractValidator<ExpireRoleAssignmentCommand>
{
    public ExpireRoleAssignmentCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.RoleAssignmentId).NotEmpty();
    }
}
