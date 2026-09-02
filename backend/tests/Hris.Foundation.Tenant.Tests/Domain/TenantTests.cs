using FluentAssertions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Tenant.Domain;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Domain;

public sealed class TenantTests
{
    [Fact]
    public void Register_Succeeds_WithValidInput()
    {
        var tenantCode = TestData.NewTenantCode();

        var result = TenantAggregate.Register(
            tenantCode, "ACME Manufacturing", SubscriptionPlan.Growth, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantCode.Should().Be(tenantCode);
        result.Value.Organization.Should().Be("ACME Manufacturing");
        result.Value.SubscriptionPlan.Should().Be(SubscriptionPlan.Growth);
    }

    [Fact]
    public void Register_EntersProvisioningDirectly_NeverPersistingAnIndependentRequestedState()
    {
        var tenant = TestData.RegisteredTenant();

        tenant.LifecycleState.Should().Be(
            TenantLifecycleState.Provisioning,
            "Requested -> Provisioning is one atomic transition per RegisterTenantCommand's own Triggers column");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenOrganizationIsNullOrWhitespace(string? organization)
    {
        var result = TenantAggregate.Register(
            TestData.NewTenantCode(), organization, SubscriptionPlan.Starter, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.OrganizationRequired);
    }

    [Fact]
    public void Register_TrimsOrganization()
    {
        var result = TenantAggregate.Register(
            TestData.NewTenantCode(), "  ACME  ", SubscriptionPlan.Starter, null, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Organization.Should().Be("ACME");
    }

    [Fact]
    public void Register_RaisesTenantCreatedEvent_WithCorrectData()
    {
        var tenantCode = TestData.NewTenantCode();
        var registeredBy = TestData.NewPlatformOperatorId();

        var tenant = TenantAggregate.Register(
            tenantCode, "ACME", SubscriptionPlan.Enterprise, registeredBy, TestData.NowUtc).Value;

        tenant.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TenantCreated>()
            .Which.Should().BeEquivalentTo(new
            {
                TenantId = tenant.Id,
                TenantCode = tenantCode,
                Organization = "ACME",
                SubscriptionPlan = SubscriptionPlan.Enterprise,
                RegisteredBy = registeredBy,
            });
    }

    [Fact]
    public void Register_RaisesTenantCreatedEvent_WithNullRegisteredBy_ForSelfServiceRegistration()
    {
        var tenant = TenantAggregate.Register(
            TestData.NewTenantCode(), "ACME", SubscriptionPlan.Starter, null, TestData.NowUtc).Value;

        tenant.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TenantCreated>()
            .Which.RegisteredBy.Should().BeNull("the self-service Starter path submits no Platform Operator actor");
    }

    [Fact]
    public void CompleteProvisioning_Succeeds_FromProvisioning_AndEntersConfigured()
    {
        var tenant = TestData.RegisteredTenant();
        var tenantConfigurationId = TestData.NewTenantConfigurationId();

        var result = tenant.CompleteProvisioning(tenantConfigurationId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Configured);
    }

    [Fact]
    public void CompleteProvisioning_RaisesTenantProvisionedEvent_WithCorrectData()
    {
        var tenant = TestData.RegisteredTenant();
        var tenantConfigurationId = TestData.NewTenantConfigurationId();

        tenant.CompleteProvisioning(tenantConfigurationId, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantProvisioned>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { TenantId = tenant.Id, TenantConfigurationId = tenantConfigurationId });
    }

    [Fact]
    public void CompleteProvisioning_Fails_WhenTenantConfigurationIdIsDefault()
    {
        var tenant = TestData.RegisteredTenant();

        var result = tenant.CompleteProvisioning(default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue(
            "a Tenant cannot reach Active without a TenantConfiguration already existing for it");
        result.Error.Should().Be(TenantErrors.TenantConfigurationIdRequired);
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Provisioning);
    }

    [Fact]
    public void CompleteProvisioning_Fails_WhenNotInProvisioning()
    {
        var tenant = TestData.ConfiguredTenant();

        var result = tenant.CompleteProvisioning(TestData.NewTenantConfigurationId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.InvalidLifecycleTransition);
    }

    [Fact]
    public void Activate_Succeeds_FromConfigured_AndEntersActive()
    {
        var tenant = TestData.ConfiguredTenant();

        var result = tenant.Activate(TestData.NewUserAccountId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Active);
    }

    [Fact]
    public void Activate_RaisesTenantActivatedEvent_WithTheAcceptingAdministratorsAccount()
    {
        var tenant = TestData.ConfiguredTenant();
        var activatedBy = TestData.NewUserAccountId();

        tenant.Activate(activatedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantActivated>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { TenantId = tenant.Id, ActivatedBy = activatedBy });
    }

    [Fact]
    public void Activate_Fails_WhenActivatedByIsDefault()
    {
        var tenant = TestData.ConfiguredTenant();

        var result = tenant.Activate(default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ActivatedByRequired);
    }

    [Theory]
    [InlineData(TenantLifecycleState.Provisioning)]
    [InlineData(TenantLifecycleState.Active)]
    [InlineData(TenantLifecycleState.Suspended)]
    [InlineData(TenantLifecycleState.Archived)]
    public void Activate_Fails_WhenNotConfigured(TenantLifecycleState state)
    {
        var tenant = TenantFor(state);

        var result = tenant.Activate(TestData.NewUserAccountId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.InvalidLifecycleTransition);
    }

    [Fact]
    public void Suspend_Succeeds_FromActive_AndEntersSuspended()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Suspend("Non-payment", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Suspended);
    }

    [Fact]
    public void Suspend_RaisesTenantSuspendedEvent_WithCorrectData()
    {
        var tenant = TestData.ActiveTenant();
        var suspendedBy = TestData.NewPlatformOperatorId();

        tenant.Suspend("Non-payment", suspendedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantSuspended>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { TenantId = tenant.Id, SuspendedBy = suspendedBy, Reason = "Non-payment" });
    }

    [Fact]
    public void Suspend_Fails_WhenNotActive()
    {
        var tenant = TestData.ConfiguredTenant();

        var result = tenant.Suspend("Reason", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.InvalidLifecycleTransition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Suspend_Fails_WhenReasonIsNullOrWhitespace(string? reason)
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Suspend(reason, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ReasonRequired);
    }

    [Fact]
    public void Suspend_Fails_WhenSuspendedByIsDefault()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Suspend("Reason", default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Fact]
    public void Reactivate_Succeeds_FromSuspended_AndEntersActiveDirectly()
    {
        var tenant = TestData.SuspendedTenant();

        var result = tenant.Reactivate(TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(
            TenantLifecycleState.Active,
            "Reactivated names the transition event, not a second persisted state a tenant waits in");
    }

    [Fact]
    public void Reactivate_RaisesTenantReactivatedEvent_WithCorrectData()
    {
        var tenant = TestData.SuspendedTenant();
        var reactivatedBy = TestData.NewPlatformOperatorId();

        tenant.Reactivate(reactivatedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantReactivated>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { TenantId = tenant.Id, ReactivatedBy = reactivatedBy });
    }

    [Fact]
    public void Reactivate_Fails_WhenNotSuspended()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Reactivate(TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.InvalidLifecycleTransition);
    }

    [Fact]
    public void Reactivate_Fails_WhenReactivatedByIsDefault()
    {
        var tenant = TestData.SuspendedTenant();

        var result = tenant.Reactivate(default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Fact]
    public void Archive_Succeeds_FromActive_AndEntersArchived()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Archive("Churned", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Archived);
    }

    [Fact]
    public void Archive_RaisesTenantArchivedEvent_WithCorrectData()
    {
        var tenant = TestData.ActiveTenant();
        var archivedBy = TestData.NewPlatformOperatorId();

        tenant.Archive("Churned", archivedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantArchived>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { TenantId = tenant.Id, ArchivedBy = archivedBy, Reason = "Churned" });
    }

    [Fact]
    public void Archive_Fails_WhenSuspended_BecauseTheStateMachineHasNoDirectSuspendedToArchivedEdge()
    {
        var tenant = TestData.SuspendedTenant();

        var result = tenant.Archive("Churned", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue(
            "the State Machine table sources Archived from Active only -- a Suspended tenant must Reactivate first");
        result.Error.Should().Be(TenantErrors.InvalidLifecycleTransition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Archive_Fails_WhenReasonIsNullOrEmpty(string? reason)
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Archive(reason, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ReasonRequired);
    }

    [Fact]
    public void Archive_NeverDeletesData_ItOnlyChangesLifecycleState()
    {
        var tenant = TestData.ActiveTenant();
        var organizationBeforeArchiving = tenant.Organization;
        var subscriptionPlanBeforeArchiving = tenant.SubscriptionPlan;

        tenant.Archive("Churned", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        tenant.Organization.Should().Be(organizationBeforeArchiving);
        tenant.SubscriptionPlan.Should().Be(subscriptionPlanBeforeArchiving);
    }

    [Fact]
    public void Archive_Fails_WhenArchivedByIsDefault()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.Archive("Churned", default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Fact]
    public void Delete_Succeeds_FromArchived_AndEntersDeleted()
    {
        var tenant = TestData.ArchivedTenant();

        var result = tenant.Delete("Retention elapsed", "RA 10173 request", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Deleted);
    }

    [Fact]
    public void Delete_RaisesTenantDeletedEvent_WithCorrectData()
    {
        var tenant = TestData.ArchivedTenant();
        var deletedBy = TestData.NewPlatformOperatorId();

        tenant.Delete("Retention elapsed", "RA 10173 request", deletedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantDeleted>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                TenantId = tenant.Id,
                DeletedBy = deletedBy,
                Reason = "Retention elapsed",
                ComplianceBasis = "RA 10173 request",
            });
    }

    [Theory]
    [InlineData(TenantLifecycleState.Provisioning)]
    [InlineData(TenantLifecycleState.Configured)]
    [InlineData(TenantLifecycleState.Active)]
    [InlineData(TenantLifecycleState.Suspended)]
    public void Delete_Fails_UnlessAlreadyArchived(TenantLifecycleState state)
    {
        var tenant = TenantFor(state);

        var result = tenant.Delete("Reason", "Basis", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue(
            "deletion is always a deliberate second step after archival, never a single action collapsing both");
        result.Error.Should().Be(TenantErrors.DeleteRequiresArchived);
    }

    [Fact]
    public void Delete_Fails_WhenComplianceBasisIsMissing()
    {
        var tenant = TestData.ArchivedTenant();

        var result = tenant.Delete("Reason", null, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ComplianceBasisRequired);
    }

    [Fact]
    public void Delete_Fails_WhenDeletedByIsDefault()
    {
        var tenant = TestData.ArchivedTenant();

        var result = tenant.Delete("Reason", "Basis", default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Fact]
    public void ChangeSubscriptionPlan_Succeeds_WhileActive()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.ChangeSubscriptionPlan(SubscriptionPlan.Enterprise, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.SubscriptionPlan.Should().Be(SubscriptionPlan.Enterprise);
    }

    [Fact]
    public void ChangeSubscriptionPlan_RaisesTenantLicenseUpdatedEvent_WithPreviousAndNewPlan_AndEmptyPacksActivated()
    {
        var tenant = TestData.ActiveTenant();
        var changedBy = TestData.NewPlatformOperatorId();
        var previousPlan = tenant.SubscriptionPlan;

        tenant.ChangeSubscriptionPlan(SubscriptionPlan.Government, changedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantLicenseUpdated>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                TenantId = tenant.Id,
                PreviousSubscriptionPlan = previousPlan,
                NewSubscriptionPlan = SubscriptionPlan.Government,
                ChangedBy = changedBy,
                PacksActivated = Array.Empty<string>(),
            });
    }

    [Theory]
    [InlineData(TenantLifecycleState.Provisioning)]
    [InlineData(TenantLifecycleState.Configured)]
    [InlineData(TenantLifecycleState.Suspended)]
    [InlineData(TenantLifecycleState.Archived)]
    public void ChangeSubscriptionPlan_Fails_WhenNotActive(TenantLifecycleState state)
    {
        var tenant = TenantFor(state);

        var result = tenant.ChangeSubscriptionPlan(SubscriptionPlan.Enterprise, TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.SubscriptionPlanChangeRequiresActive);
    }

    [Fact]
    public void ChangeSubscriptionPlan_Fails_WhenChangedByIsDefault()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.ChangeSubscriptionPlan(SubscriptionPlan.Enterprise, default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Theory]
    [InlineData(TenantLifecycleState.Provisioning)]
    [InlineData(TenantLifecycleState.Configured)]
    [InlineData(TenantLifecycleState.Active)]
    [InlineData(TenantLifecycleState.Suspended)]
    [InlineData(TenantLifecycleState.Archived)]
    public void UpdateOrganizationName_Succeeds_InAnyStateExceptDeleted(TenantLifecycleState state)
    {
        var tenant = TenantFor(state);

        var result = tenant.UpdateOrganizationName("New Name Inc.", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        tenant.Organization.Should().Be("New Name Inc.");
    }

    [Fact]
    public void UpdateOrganizationName_Fails_WhenUpdatedByIsDefault()
    {
        var tenant = TestData.ActiveTenant();

        var result = tenant.UpdateOrganizationName("New Name Inc.", default, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.PlatformOperatorRequired);
    }

    [Fact]
    public void UpdateOrganizationName_Fails_WhenDeleted()
    {
        var tenant = TestData.DeletedTenant();

        var result = tenant.UpdateOrganizationName("New Name Inc.", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.OrganizationNameUpdateRejectedWhenDeleted);
    }

    [Fact]
    public void UpdateOrganizationName_RaisesTenantUpdatedEvent_WithPreviousAndNewOrganization()
    {
        var tenant = TestData.ActiveTenant();
        var previousOrganization = tenant.Organization;
        var updatedBy = TestData.NewPlatformOperatorId();

        tenant.UpdateOrganizationName("New Name Inc.", updatedBy, TestData.NowUtc);

        tenant.DomainEvents.OfType<TenantUpdated>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                TenantId = tenant.Id,
                PreviousOrganization = previousOrganization,
                NewOrganization = "New Name Inc.",
                UpdatedBy = updatedBy,
            });
    }

    [Fact]
    public void UpdateOrganizationName_NeverAcceptsATenantCodeChange_TenantCodeIsImmutable()
    {
        var tenant = TestData.ActiveTenant();
        var originalTenantCode = tenant.TenantCode;

        tenant.UpdateOrganizationName("New Name Inc.", TestData.NewPlatformOperatorId(), TestData.NowUtc);

        tenant.TenantCode.Should().Be(originalTenantCode, "no method on Tenant accepts a TenantCode parameter after Register");
    }

    /// <summary>
    /// Builds a pre-existing fixture in the given state, reusing the real, already
    /// public <see cref="TenantLifecycleState"/> as the <c>[Theory]</c>/
    /// <c>[InlineData]</c> discriminator rather than a second, test-only enum --
    /// <see cref="TenantLifecycleState.Requested"/> and
    /// <see cref="TenantLifecycleState.Deleted"/> are never passed by any Theory in
    /// this file (Requested is never an independently observable fixture, per
    /// Register's own remarks; Deleted has its own dedicated
    /// <see cref="TestData.DeletedTenant"/> used directly where needed), so both fall
    /// through to the exception below rather than needing their own case.
    /// </summary>
    private static TenantAggregate TenantFor(TenantLifecycleState state) => state switch
    {
        TenantLifecycleState.Provisioning => TestData.RegisteredTenant(),
        TenantLifecycleState.Configured => TestData.ConfiguredTenant(),
        TenantLifecycleState.Active => TestData.ActiveTenant(),
        TenantLifecycleState.Suspended => TestData.SuspendedTenant(),
        TenantLifecycleState.Archived => TestData.ArchivedTenant(),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
