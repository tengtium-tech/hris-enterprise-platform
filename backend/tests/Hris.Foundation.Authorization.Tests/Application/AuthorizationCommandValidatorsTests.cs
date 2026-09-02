using FluentAssertions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Application.Validators;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator -- exercising each validator's
/// own field-level contract, not re-testing FluentValidation's own NotEmpty/IsInEnum
/// mechanics (already covered by that library's own test suite). Each validator here
/// deliberately checks nothing the Domain layer's own factory/transition methods
/// already enforce, per that validators file's own remarks -- scope-id requirement,
/// expiration-before-effective-date, and Auditor mutation-permission rejection are
/// covered by <see cref="Domain.OrganizationalScopeTests"/>,
/// <see cref="Domain.RoleAssignmentTests"/>, and
/// <see cref="Domain.RolePermissionGrantTests"/> instead.
/// </summary>
public sealed class AuthorizationCommandValidatorsTests
{
    [Fact]
    public void AssignRoleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPrincipalId()
    {
        var validator = new AssignRoleCommandValidator();
        var valid = new AssignRoleCommand(
            Guid.NewGuid(), Role.HRManager, OrganizationalScopeLevel.Tenant, Guid.NewGuid(),
            RoleAssignmentType.Direct, TestData.Today, null, Guid.NewGuid());
        var invalid = valid with { PrincipalId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RevokeRoleAssignmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RevokeRoleAssignmentCommandValidator();

        validator.Validate(new RevokeRoleAssignmentCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RevokeRoleAssignmentCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GrantPermissionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyResourceType()
    {
        var validator = new GrantPermissionCommandValidator();

        validator.Validate(new GrantPermissionCommand(Role.HRManager, "Employee", PermissionAction.Read)).IsValid.Should().BeTrue();
        validator.Validate(new GrantPermissionCommand(Role.HRManager, string.Empty, PermissionAction.Read)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RevokePermissionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RevokePermissionCommandValidator();

        validator.Validate(new RevokePermissionCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RevokePermissionCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CheckAuthorizationQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyResourceType()
    {
        var validator = new CheckAuthorizationQueryValidator();
        var valid = new CheckAuthorizationQuery(Guid.NewGuid(), "Employee", PermissionAction.Read, OrganizationalScopeLevel.Tenant, Guid.NewGuid());
        var invalid = valid with { ResourceType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetRoleAssignmentsForPrincipalQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyPrincipalId()
    {
        var validator = new GetRoleAssignmentsForPrincipalQueryValidator();

        validator.Validate(new GetRoleAssignmentsForPrincipalQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetRoleAssignmentsForPrincipalQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetActivePermissionsForRoleQueryValidator_AcceptsAValidQuery()
    {
        var validator = new GetActivePermissionsForRoleQueryValidator();

        validator.Validate(new GetActivePermissionsForRoleQuery(Role.HRManager)).IsValid.Should().BeTrue();
    }
}
