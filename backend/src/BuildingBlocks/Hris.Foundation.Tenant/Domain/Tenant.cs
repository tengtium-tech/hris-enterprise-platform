using System.Diagnostics.CodeAnalysis;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Aggregate Root of the Tenant Framework: the one aggregate every other aggregate on
/// the platform -- all nineteen business modules', plus `administration`'s own
/// TenantConfiguration and IntegrationCredential -- references by TenantId without
/// exception. Source: docs/03-foundation/tenant-framework.md, Tenant Aggregate
/// section, which formalizes what that document's earlier sections only described in
/// prose.
///
/// First framework built in Sprint 4 (docs/00-project's own IMPLEMENTATION-PLAN.md
/// Sprint 4 row: "no forced order... all eight are equally ready"), and the first
/// framework this session builds with no pre-existing Domain layer at all.
///
/// Owns exactly four fields: <see cref="TenantCode"/> (globally unique, immutable),
/// <see cref="Organization"/>, <see cref="SubscriptionPlan"/>, and
/// <see cref="LifecycleState"/>. Deliberately does NOT own Branding, Numbering-format
/// defaults, Process Pack Activations, Default Locale/Currency/Time Zone, or
/// Integration Credentials -- all `administration`'s own TenantConfiguration or
/// IntegrationCredential aggregates (Tenant Aggregate, Does Not Own). Neither exists
/// in code yet: `administration` is a Phase 2 business module, and this Sprint builds
/// Foundation frameworks only. Two consequences of that gap, both deliberate and
/// documented at the point they bite rather than worked around silently:
/// <see cref="CompleteProvisioning"/> requires a <see cref="TenantConfigurationId"/>
/// as proof one was created, without this aggregate ever creating one itself; and
/// <see cref="ChangeSubscriptionPlan"/> changes only this aggregate's own
/// SubscriptionPlan field, never touching Process Pack Activation state it does not
/// own or have any way to reach.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Tenant\" is tenant-framework.md's own ubiquitous-language name for " +
        "this Aggregate Root (\"Root: Tenant, identified by TenantId\") and this project's " +
        "own name (Hris.Foundation.Tenant) matches every sibling Hris.Foundation.<Framework> " +
        "project in this solution -- renaming either would break one of those two established " +
        "conventions to satisfy a naming lint. Every reference to this class outside the " +
        "Domain namespace itself uses the TenantAggregate alias (see TenantMapper's own " +
        "remarks for the full reasoning), which is the actual, load-bearing fix for the " +
        "ambiguity this rule is warning about -- not renaming the type.")]
public sealed class Tenant : AggregateRoot<TenantId>
{
    public TenantCode TenantCode { get; }

    public string Organization { get; private set; }

    public SubscriptionPlan SubscriptionPlan { get; private set; }

    public TenantLifecycleState LifecycleState { get; private set; }

    private Tenant(TenantId id, TenantCode tenantCode, string organization, SubscriptionPlan subscriptionPlan)
        : base(id)
    {
        TenantCode = tenantCode;
        Organization = organization;
        SubscriptionPlan = subscriptionPlan;

        // Requested -> Provisioning is one atomic transition per RegisterTenantCommand's
        // own Triggers column (State Machine, above) -- Requested is never an
        // independently observable or persisted state.
        LifecycleState = TenantLifecycleState.Provisioning;
    }

    /// <summary>
    /// Registers a new tenant. Corresponds to <c>RegisterTenantCommand</c>
    /// (Platform-Operator-Facing Commands and Queries) -- both the vendor-assisted
    /// path (Platform Operator supplies <paramref name="registeredBy"/>) and the
    /// self-service Starter-edition path (<paramref name="registeredBy"/> is
    /// <c>null</c>) resolve to this same factory, per that section's own "the
    /// difference is who submits the command, never what the command does once
    /// submitted."
    ///
    /// Tenant Code global uniqueness is checked by the caller before this factory
    /// runs (<see cref="ITenantRepository.ExistsByTenantCodeAsync"/>), not here -- a
    /// Value Object/Aggregate factory validates shape, not cross-aggregate state
    /// (the same split <c>CreateCountryConfigurationCommandHandler</c> already
    /// establishes).
    ///
    /// Deliberately takes no Default Locale, Default Currency, Time Zone, or initial
    /// Tenant Administrator name/email parameters, even though
    /// <c>RegisterTenantCommand</c>'s own Carries column lists them: those are
    /// registration-time inputs consumed once to seed `administration`'s own
    /// TenantConfiguration (Does Not Own, above), never persisted as this aggregate's
    /// own fields. The command handler that calls this factory accepts and carries
    /// those fields (matching the document's own shape for forward compatibility) but
    /// has nothing to do with them yet, since TenantConfiguration does not exist in
    /// code -- see that handler's own remarks.
    /// </summary>
    public static Result<Tenant> Register(
        TenantCode tenantCode,
        string? organization,
        SubscriptionPlan subscriptionPlan,
        PlatformOperatorId? registeredBy,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(tenantCode, nameof(tenantCode));

        if (string.IsNullOrWhiteSpace(organization))
        {
            return Result.Failure<Tenant>(TenantErrors.OrganizationRequired);
        }

        var tenant = new Tenant(new TenantId(Guid.NewGuid()), tenantCode, organization.Trim(), subscriptionPlan);

        tenant.AddDomainEvent(new TenantCreated(
            Guid.NewGuid(), nowUtc, tenant.Id, tenantCode, tenant.Organization, subscriptionPlan, registeredBy));

        return Result.Success(tenant);
    }

    /// <summary>
    /// Provisioning -> Configured, per the State Machine's own "Automated provisioning
    /// completes (Database Initialization, Configuration Creation, Branding
    /// Initialization, Process Pack Activation, administrator invitation dispatched)"
    /// row -- the one Tenant Lifecycle transition the document names with no Platform
    /// Operator command behind it at all, because it is orchestration across several
    /// systems, not a single actor's request.
    ///
    /// This method is deliberately narrow: it is the mechanical trigger for the
    /// already-fully-specified <see cref="TenantProvisioned"/> event (exact field
    /// list: TenantId, TenantConfigurationId), requiring proof a TenantConfiguration
    /// already exists -- the structural enforcement the Tenant Aggregate's own
    /// Invariants require ("A Tenant cannot reach Active without a TenantConfiguration
    /// already existing for it"). It does not itself perform Database Initialization,
    /// Configuration Creation, Branding Initialization, Process Pack Activation, or
    /// invitation dispatch -- none of those systems (`administration` module,
    /// Entitlement &amp; Process Pack Framework, Notification Framework) exist in code
    /// yet (IMPLEMENTATION-PLAN.md Sprints 6 and 5, and Phase 2, respectively). The
    /// orchestration that calls this method once those steps genuinely complete is
    /// future work for whichever of those systems lands last; wiring it in
    /// incrementally as siblings come online is this session's own established
    /// pattern, not a gap unique to this method.
    /// </summary>
    public Result CompleteProvisioning(TenantConfigurationId tenantConfigurationId, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Provisioning)
        {
            return Result.Failure(TenantErrors.InvalidLifecycleTransition);
        }

        if (tenantConfigurationId.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.TenantConfigurationIdRequired);
        }

        LifecycleState = TenantLifecycleState.Configured;
        AddDomainEvent(new TenantProvisioned(Guid.NewGuid(), nowUtc, Id, tenantConfigurationId));
        return Result.Success();
    }

    /// <summary>
    /// Configured -> Active. <paramref name="activatedBy"/> is the invited Tenant
    /// Administrator's own <see cref="UserAccountId"/>, never a
    /// <see cref="PlatformOperatorId"/> -- <c>ActivateTenantCommand</c> is this
    /// document's own stated exception among the eight Platform-Operator-Facing
    /// commands ("invoked by the invited Tenant Administrator, not a Platform
    /// Operator... the Platform Operator's own involvement ends here"). By the time
    /// this method can succeed, <see cref="LifecycleState"/> is already
    /// <see cref="TenantLifecycleState.Configured"/>, which only
    /// <see cref="CompleteProvisioning"/> reaches, and only with a real
    /// TenantConfigurationId already supplied -- so the "TenantConfiguration already
    /// exists" half of the Active precondition is structurally guaranteed by the state
    /// itself, not re-checked here.
    /// </summary>
    public Result Activate(UserAccountId activatedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Configured)
        {
            return Result.Failure(TenantErrors.InvalidLifecycleTransition);
        }

        if (activatedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.ActivatedByRequired);
        }

        LifecycleState = TenantLifecycleState.Active;
        AddDomainEvent(new TenantActivated(Guid.NewGuid(), nowUtc, Id, activatedBy));
        return Result.Success();
    }

    public Result Suspend(string? reason, PlatformOperatorId suspendedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Active)
        {
            return Result.Failure(TenantErrors.InvalidLifecycleTransition);
        }

        if (suspendedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(TenantErrors.ReasonRequired);
        }

        LifecycleState = TenantLifecycleState.Suspended;
        AddDomainEvent(new TenantSuspended(Guid.NewGuid(), nowUtc, Id, suspendedBy, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Suspended -> Active directly, per the State Machine's own "Suspended →
    /// Reactivated → Active" line: "Reactivated names the transition event, not a
    /// second persisted state a tenant waits in." There is no
    /// <c>TenantLifecycleState.Reactivated</c> member to set.
    /// </summary>
    public Result Reactivate(PlatformOperatorId reactivatedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Suspended)
        {
            return Result.Failure(TenantErrors.InvalidLifecycleTransition);
        }

        if (reactivatedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        LifecycleState = TenantLifecycleState.Active;
        AddDomainEvent(new TenantReactivated(Guid.NewGuid(), nowUtc, Id, reactivatedBy));
        return Result.Success();
    }

    /// <summary>
    /// Active -> Archived only, per the State Machine table's own single sourced row
    /// for this transition -- a Suspended tenant has no direct path to Archived in
    /// this document; it must Reactivate first. Never deletes data (Invariants:
    /// "Archiving a Tenant never deletes its data").
    /// </summary>
    public Result Archive(string? reason, PlatformOperatorId archivedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Active)
        {
            return Result.Failure(TenantErrors.InvalidLifecycleTransition);
        }

        if (archivedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(TenantErrors.ReasonRequired);
        }

        LifecycleState = TenantLifecycleState.Archived;
        AddDomainEvent(new TenantArchived(Guid.NewGuid(), nowUtc, Id, archivedBy, reason.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Archived -> Deleted only -- rejected from every other state, per Invariants:
    /// "DeleteTenantCommand is rejected unless the Tenant is already Archived...
    /// deletion is always a deliberate second step." The retention window a Platform
    /// Operator must wait before this is callable is a deliberately open policy
    /// question this document leaves unanswered (Platform-Operator-Facing Commands and
    /// Queries, DeleteTenantCommand's own retention-gate note) -- enforcing a specific
    /// duration here would be inventing a number the specification does not give.
    /// </summary>
    public Result Delete(string? reason, string? complianceBasis, PlatformOperatorId deletedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Archived)
        {
            return Result.Failure(TenantErrors.DeleteRequiresArchived);
        }

        if (deletedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(TenantErrors.ReasonRequired);
        }

        if (string.IsNullOrWhiteSpace(complianceBasis))
        {
            return Result.Failure(TenantErrors.ComplianceBasisRequired);
        }

        LifecycleState = TenantLifecycleState.Deleted;
        AddDomainEvent(new TenantDeleted(Guid.NewGuid(), nowUtc, Id, deletedBy, reason.Trim(), complianceBasis.Trim()));
        return Result.Success();
    }

    /// <summary>
    /// Active only, per the Platform-Operator-Facing Commands table's own
    /// ChangeTenantSubscriptionPlanCommand row. Changes only this aggregate's own
    /// SubscriptionPlan field -- <see cref="TenantLicenseUpdated.PacksActivated"/> is
    /// always empty here, because activating any pack the target edition newly
    /// includes by default, and rejecting a downgrade that would remove a pack the
    /// tenant already holds, are both `administration`'s own TenantConfiguration
    /// responsibility (Does Not Own, above) -- this aggregate has no Process Pack
    /// Activation state to read or change. A caller integrating with
    /// TenantConfiguration once it exists enforces those two rules there, in the same
    /// operation, before or after calling this method; this method cannot enforce a
    /// rule about data it structurally cannot see.
    /// </summary>
    public Result ChangeSubscriptionPlan(SubscriptionPlan newSubscriptionPlan, PlatformOperatorId changedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState != TenantLifecycleState.Active)
        {
            return Result.Failure(TenantErrors.SubscriptionPlanChangeRequiresActive);
        }

        if (changedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        var previousPlan = SubscriptionPlan;
        SubscriptionPlan = newSubscriptionPlan;

        AddDomainEvent(new TenantLicenseUpdated(
            Guid.NewGuid(), nowUtc, Id, previousPlan, newSubscriptionPlan, changedBy, Array.Empty<string>()));

        return Result.Success();
    }

    /// <summary>
    /// Any state except Deleted, per the State Machine's own note on this command.
    /// Never accepts or changes Tenant Code -- Invariants: "Tenant Code is immutable
    /// once set... no command in this document ever changes it after
    /// RegisterTenantCommand."
    /// </summary>
    public Result UpdateOrganizationName(string? newOrganization, PlatformOperatorId updatedBy, DateTimeOffset nowUtc)
    {
        if (LifecycleState == TenantLifecycleState.Deleted)
        {
            return Result.Failure(TenantErrors.OrganizationNameUpdateRejectedWhenDeleted);
        }

        if (updatedBy.Value == Guid.Empty)
        {
            return Result.Failure(TenantErrors.PlatformOperatorRequired);
        }

        if (string.IsNullOrWhiteSpace(newOrganization))
        {
            return Result.Failure(TenantErrors.OrganizationRequired);
        }

        var previousOrganization = Organization;
        Organization = newOrganization.Trim();

        AddDomainEvent(new TenantUpdated(Guid.NewGuid(), nowUtc, Id, previousOrganization, Organization, updatedBy));
        return Result.Success();
    }
}
