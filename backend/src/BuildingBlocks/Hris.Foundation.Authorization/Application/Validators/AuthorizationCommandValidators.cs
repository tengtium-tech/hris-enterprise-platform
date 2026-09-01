using FluentValidation;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Application.Queries;

namespace Hris.Foundation.Authorization.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields... Business-
/// independent validation." Deliberately does not re-check anything the Domain
/// layer's own factory/transition methods already enforce (scope-id requirement,
/// expiration-before-effective-date, Auditor mutation-permission rejection) -- the
/// identical separation every other Sprint 3 framework's own validators state.
/// </summary>
public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(c => c.PrincipalId).NotEmpty();
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
        RuleFor(c => c.AssignmentType).IsInEnum();
        RuleFor(c => c.GrantedByPrincipalId).NotEmpty();
    }
}

public sealed class RevokeRoleAssignmentCommandValidator : AbstractValidator<RevokeRoleAssignmentCommand>
{
    public RevokeRoleAssignmentCommandValidator()
    {
        RuleFor(c => c.RoleAssignmentId).NotEmpty();
    }
}

public sealed class GrantPermissionCommandValidator : AbstractValidator<GrantPermissionCommand>
{
    public GrantPermissionCommandValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.ResourceType).NotEmpty();
        RuleFor(c => c.Action).IsInEnum();
    }
}

public sealed class RevokePermissionCommandValidator : AbstractValidator<RevokePermissionCommand>
{
    public RevokePermissionCommandValidator()
    {
        RuleFor(c => c.RolePermissionGrantId).NotEmpty();
    }
}

public sealed class CheckAuthorizationQueryValidator : AbstractValidator<CheckAuthorizationQuery>
{
    public CheckAuthorizationQueryValidator()
    {
        RuleFor(c => c.PrincipalId).NotEmpty();
        RuleFor(c => c.ResourceType).NotEmpty();
        RuleFor(c => c.Action).IsInEnum();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class GetRoleAssignmentsForPrincipalQueryValidator : AbstractValidator<GetRoleAssignmentsForPrincipalQuery>
{
    public GetRoleAssignmentsForPrincipalQueryValidator()
    {
        RuleFor(c => c.PrincipalId).NotEmpty();
    }
}

public sealed class GetActivePermissionsForRoleQueryValidator : AbstractValidator<GetActivePermissionsForRoleQuery>
{
    public GetActivePermissionsForRoleQueryValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
    }
}
