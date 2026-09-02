using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Application.Queries;
using Hris.Foundation.Tenant.Application.Validators;
using Hris.Foundation.Tenant.Domain;
using Xunit;

namespace Hris.Foundation.Tenant.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>AuthorizationCommandValidatorsTests</c> already establishes -- exercising each
/// validator's own field-level contract, not FluentValidation's own NotEmpty
/// mechanics. Deliberately does not re-test anything the Domain layer's own
/// factory/transition methods already enforce (tenant code format, lifecycle-state
/// gating), per <c>TenantCommandValidators</c>'s own remarks -- those are covered by
/// <see cref="Domain.TenantCodeTests"/> and <see cref="Domain.TenantTests"/> instead.
/// </summary>
public sealed class TenantCommandValidatorsTests
{
    [Fact]
    public void RegisterTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantCode()
    {
        var validator = new RegisterTenantCommandValidator();
        var valid = new RegisterTenantCommand(
            "acme-corp", "ACME", SubscriptionPlan.Starter, "en-PH", "PHP", "Asia/Manila", "Jane", "jane@acme.example", null);
        var invalid = valid with { TenantCode = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteTenantProvisioningCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantConfigurationId()
    {
        var validator = new CompleteTenantProvisioningCommandValidator();

        validator.Validate(new CompleteTenantProvisioningCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new CompleteTenantProvisioningCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyActivatedBy()
    {
        var validator = new ActivateTenantCommandValidator();

        validator.Validate(new ActivateTenantCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ActivateTenantCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SuspendTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new SuspendTenantCommandValidator();

        validator.Validate(new SuspendTenantCommand(Guid.NewGuid(), "Non-payment", Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new SuspendTenantCommand(Guid.NewGuid(), string.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReactivateTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReactivatedBy()
    {
        var validator = new ReactivateTenantCommandValidator();

        validator.Validate(new ReactivateTenantCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ReactivateTenantCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new ArchiveTenantCommandValidator();

        validator.Validate(new ArchiveTenantCommand(Guid.NewGuid(), "Churned", Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ArchiveTenantCommand(Guid.NewGuid(), string.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeleteTenantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyComplianceBasis()
    {
        var validator = new DeleteTenantCommandValidator();
        var valid = new DeleteTenantCommand(Guid.NewGuid(), "Reason", "RA 10173 request", Guid.NewGuid());
        var invalid = valid with { ComplianceBasis = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangeTenantSubscriptionPlanCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyChangedBy()
    {
        var validator = new ChangeTenantSubscriptionPlanCommandValidator();

        validator.Validate(new ChangeTenantSubscriptionPlanCommand(Guid.NewGuid(), SubscriptionPlan.Growth, Guid.NewGuid()))
            .IsValid.Should().BeTrue();
        validator.Validate(new ChangeTenantSubscriptionPlanCommand(Guid.NewGuid(), SubscriptionPlan.Growth, Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTenantOrganizationNameCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNewOrganization()
    {
        var validator = new UpdateTenantOrganizationNameCommandValidator();
        var valid = new UpdateTenantOrganizationNameCommand(Guid.NewGuid(), "New Name Inc.", Guid.NewGuid());
        var invalid = valid with { NewOrganization = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetTenantQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new GetTenantQueryValidator();

        validator.Validate(new GetTenantQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetTenantQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
