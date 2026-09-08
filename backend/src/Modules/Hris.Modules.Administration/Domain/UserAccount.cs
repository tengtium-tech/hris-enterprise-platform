using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Aggregate Root representing a tenant's record of a platform user: its type, its
/// optional employee linkage, and the roles it holds. Source:
/// docs/04-modules/administration/domain/aggregates.md and platform-users.md, both
/// of which name this the module's own primary Aggregate Root.
///
/// A user account is not a person and not an employee record (platform-users.md's
/// own governing distinction) -- most employees never hold one, and the `employee`
/// module has no dependency on accounts existing. <see cref="EmployeeId"/> is a
/// plain <see cref="Guid"/>, never a compile-time reference to the Employee
/// module's own type, per this platform's own standing "reference by identifier,
/// never by instance" rule.
///
/// Role assignments live inside this Aggregate, not as a separate root, because
/// separation of duties (AR-003) requires the complete assignment set as one
/// consistency boundary -- see <see cref="RoleAssignment"/>'s own remarks and
/// aggregates.md's "Why Role Assignment Is Not Its Own Aggregate".
///
/// AR-001, AR-002, and AR-003 -- self-grant, privilege escalation, and separation
/// of duties -- are this Aggregate's security core and are enforced as invariants
/// inside <see cref="GrantRole"/> itself, reachable by no path that bypasses it
/// (direct grant, provisioning, bulk, import all call this same method).
/// </summary>
public sealed class UserAccount : AggregateRoot<UserAccountId>
{
    private readonly List<RoleAssignment> _roleAssignments = [];

    public Guid TenantId { get; }

    public AccountType AccountType { get; }

    public Guid? EmployeeId { get; }

    public UserAccountStatus Status { get; private set; }

    public DateOnly? ExpiryDate { get; }

    public Guid? ServiceAccountOwnerId { get; private set; }

    public Guid ProvisionedBy { get; }

    public DateTimeOffset ProvisionedOn { get; }

    public IReadOnlyList<RoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    private UserAccount(
        UserAccountId id, Guid tenantId, AccountType accountType, Guid? employeeId, DateOnly? expiryDate,
        Guid? serviceAccountOwnerId, Guid provisionedBy, DateTimeOffset provisionedOn)
        : base(id)
    {
        TenantId = tenantId;
        AccountType = accountType;
        EmployeeId = employeeId;
        Status = UserAccountStatus.Pending;
        ExpiryDate = expiryDate;
        ServiceAccountOwnerId = serviceAccountOwnerId;
        ProvisionedBy = provisionedBy;
        ProvisionedOn = provisionedOn;
    }

    /// <summary>
    /// Creates a new account in <see cref="UserAccountStatus.Pending"/> (an account
    /// may be provisioned, with assignments granted afterward in the same
    /// transaction, before the person can use it -- platform-users.md's "Pending"
    /// section). Initial role assignments are not accepted as a parameter here:
    /// the calling Application-layer command handler calls <see cref="GrantRole"/>
    /// once per initial assignment immediately after this method returns, still
    /// inside the same transaction, so provisioning cannot bypass AR-001/AR-002/
    /// AR-003 (AR-019) -- there is exactly one grant code path, not a second one
    /// reachable only from provisioning.
    /// </summary>
    public static Result<UserAccount> Create(
        UserAccountId id, Guid tenantId, AccountType accountType, Guid? employeeId, DateOnly? expiryDate,
        Guid? serviceAccountOwnerId, Guid provisionedBy, DateTimeOffset nowUtc)
    {
        switch (accountType)
        {
            case AccountType.EmployeeLinked when employeeId is null:
                return Result.Failure<UserAccount>(AdministrationErrors.EmployeeIdRequiredForEmployeeLinkedAccount);
            case AccountType.External or AccountType.Service when employeeId is not null:
                return Result.Failure<UserAccount>(AdministrationErrors.EmployeeIdProhibitedForNonEmployeeLinkedAccount);
            case AccountType.External when expiryDate is null:
                return Result.Failure<UserAccount>(AdministrationErrors.ExpiryDateRequiredForExternalAccount);
            case AccountType.Service when serviceAccountOwnerId is null:
                return Result.Failure<UserAccount>(AdministrationErrors.OwnerRequiredForServiceAccount);
        }

        var account = new UserAccount(id, tenantId, accountType, employeeId, expiryDate, serviceAccountOwnerId, provisionedBy, nowUtc);
        account.AddDomainEvent(new UserAccountProvisioned(Guid.NewGuid(), nowUtc, id, tenantId, accountType, employeeId, provisionedBy));
        return Result.Success(account);
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Pending)
        {
            return Result.Failure(AdministrationErrors.AccountNotPending);
        }

        Status = UserAccountStatus.Active;
        AddDomainEvent(new UserAccountActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// AR-013: suspension preserves assignments; it blocks access without revoking
    /// authority. Idempotent on an already-suspended account, per commands.md's own
    /// "Idempotency" section. <paramref name="isLastTenantAdministrator"/> is
    /// computed by the caller only when this account currently holds an effective
    /// SystemAdministrator assignment at Tenant scope (AR-014); ignored otherwise.
    /// </summary>
    public Result Suspend(string? reason, Guid suspendedBy, bool isLastTenantAdministrator, DateTimeOffset nowUtc)
    {
        if (Status == UserAccountStatus.Suspended)
        {
            return Result.Success();
        }

        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure(AdministrationErrors.AccountNotActive);
        }

        if (isLastTenantAdministrator && HoldsEffectiveSystemAdministratorAtTenantScope(DateOnly.FromDateTime(nowUtc.UtcDateTime)))
        {
            return Result.Failure(AdministrationErrors.CannotRemoveLastTenantAdministrator);
        }

        Status = UserAccountStatus.Suspended;
        AddDomainEvent(new UserAccountSuspended(Guid.NewGuid(), nowUtc, Id, suspendedBy, reason ?? string.Empty));
        return Result.Success();
    }

    public Result Reinstate(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Suspended)
        {
            return Result.Failure(AdministrationErrors.AccountNotSuspended);
        }

        Status = UserAccountStatus.Active;
        AddDomainEvent(new UserAccountReinstated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// AR-012: terminal; revokes every active assignment in this same transaction.
    /// Idempotent on an already-deprovisioned account.
    /// <paramref name="isLastTenantAdministrator"/> is computed by the caller only
    /// when this account currently holds an effective SystemAdministrator
    /// assignment at Tenant scope (AR-014).
    /// </summary>
    public Result Deprovision(string? reason, Guid deprovisionedBy, bool isLastTenantAdministrator, DateTimeOffset nowUtc)
    {
        if (Status == UserAccountStatus.Deprovisioned)
        {
            return Result.Success();
        }

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        if (isLastTenantAdministrator && HoldsEffectiveSystemAdministratorAtTenantScope(today))
        {
            return Result.Failure(AdministrationErrors.CannotRemoveLastTenantAdministrator);
        }

        var revokedIds = new List<Guid>();
        foreach (var assignment in _roleAssignments.Where(a => a.RevokedOn is null && a.IsEffectiveOn(today)))
        {
            assignment.Revoke(today, deprovisionedBy, nowUtc);
            revokedIds.Add(assignment.Id.Value);
        }

        Status = UserAccountStatus.Deprovisioned;
        AddDomainEvent(new UserAccountDeprovisioned(Guid.NewGuid(), nowUtc, Id, deprovisionedBy, reason ?? string.Empty, revokedIds));
        return Result.Success();
    }

    /// <summary>
    /// Grants a role at a scope. The one grant code path every caller uses --
    /// direct grant, provisioning's own initial assignments, a future bulk-grant
    /// path -- so AR-001/AR-002/AR-003 cannot be bypassed by adding a second one
    /// (role-assignments.md's own "Every Path, Not Only the Grant Path").
    /// <paramref name="granterHasSufficientAuthority"/> is computed by the calling
    /// Application-layer command handler (AR-002: the granter must hold this exact
    /// role at an equal or broader scope on their own <see cref="UserAccount"/>
    /// instance, a cross-aggregate-instance fact this Aggregate cannot read for
    /// itself).
    /// </summary>
    public Result<Guid> GrantRole(
        RoleReference role, OrganizationalScope scope, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid grantedBy,
        string? reason, Guid? approvalReference, bool granterHasSufficientAuthority, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(scope);

        if (grantedBy == Id.Value)
        {
            return Result.Failure<Guid>(AdministrationErrors.SelfGrantProhibited);
        }

        if (!granterHasSufficientAuthority)
        {
            return Result.Failure<Guid>(AdministrationErrors.InsufficientAuthorityToGrant);
        }

        var reasonResult = GrantReason.Create(reason);
        if (reasonResult.IsFailure)
        {
            return Result.Failure<Guid>(reasonResult.Error);
        }

        if (_roleAssignments.Any(a => a.RevokedOn is null && a.Role == role && a.Scope == scope))
        {
            return Result.Failure<Guid>(AdministrationErrors.DuplicateActiveAssignment);
        }

        var proposedCanonicalRoles = HeldCanonicalRoles(includeRevoked: false)
            .Append(role.Kind == RoleKind.Canonical ? role.CanonicalRole : null)
            .Where(r => r.HasValue)
            .Select(r => r!.Value);

        if (ViolatesSeparationOfDuties(proposedCanonicalRoles))
        {
            return Result.Failure<Guid>(AdministrationErrors.ProhibitedRoleCombination);
        }

        var assignment = new RoleAssignment(
            new RoleAssignmentId(Guid.NewGuid()), effectiveFrom, effectiveTo, grantedBy, reasonResult.Value, approvalReference, nowUtc)
        {
            Role = role,
            Scope = scope,
        };
        _roleAssignments.Add(assignment);

        AddDomainEvent(new RoleGranted(
            Guid.NewGuid(), nowUtc, Id, assignment.Id, role.DisplayName, scope.ToString(), effectiveFrom, effectiveTo, grantedBy,
            reasonResult.Value.Value, approvalReference));

        return Result.Success(assignment.Id.Value);
    }

    /// <summary>
    /// AR-023: end-dates, never deletes. Idempotent on an already-revoked
    /// assignment. <paramref name="isLastTenantAdministrator"/> is computed by the
    /// caller only when the targeted assignment is itself an effective
    /// SystemAdministrator assignment at Tenant scope (AR-014).
    /// </summary>
    public Result RevokeRole(Guid roleAssignmentId, Guid revokedBy, string? reason, bool isLastTenantAdministrator, DateTimeOffset nowUtc)
    {
        var assignment = _roleAssignments.SingleOrDefault(a => a.Id.Value == roleAssignmentId);
        if (assignment is null)
        {
            return Result.Failure(AdministrationErrors.RoleAssignmentNotFound);
        }

        if (assignment.RevokedOn is not null)
        {
            return Result.Success();
        }

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        if (isLastTenantAdministrator && IsSystemAdministratorAtTenantScope(assignment) && assignment.IsEffectiveOn(today))
        {
            return Result.Failure(AdministrationErrors.CannotRemoveLastTenantAdministrator);
        }

        assignment.Revoke(today, revokedBy, nowUtc);
        AddDomainEvent(new RoleRevoked(
            Guid.NewGuid(), nowUtc, Id, assignment.Id, assignment.Role.DisplayName, assignment.Scope.ToString(), revokedBy,
            reason ?? string.Empty));

        return Result.Success();
    }

    /// <summary>Scheduled operation, no actor (role-assignments.md's "Automatic Expiry"); idempotent.</summary>
    public Result ExpireAssignment(Guid roleAssignmentId, DateTimeOffset nowUtc)
    {
        var assignment = _roleAssignments.SingleOrDefault(a => a.Id.Value == roleAssignmentId);
        if (assignment is null)
        {
            return Result.Failure(AdministrationErrors.RoleAssignmentNotFound);
        }

        if (assignment.IsExpired)
        {
            return Result.Success();
        }

        assignment.Expire();
        AddDomainEvent(new RoleAssignmentExpired(Guid.NewGuid(), nowUtc, Id, assignment.Id));
        return Result.Success();
    }

    public bool HoldsEffectiveSystemAdministratorAtTenantScope(DateOnly asOfDate) =>
        _roleAssignments.Any(a =>
            a.RevokedOn is null && a.IsEffectiveOn(asOfDate) && a.Role.Kind == RoleKind.Canonical
            && a.Role.CanonicalRole == CanonicalRole.SystemAdministrator && a.Scope.Level == ScopeLevel.Tenant);

    /// <summary>
    /// AR-002's own authority check for a proposed grant: does this account
    /// (acting as granter) already hold <paramref name="role"/> at
    /// <paramref name="scope"/> or broader. The calling Application-layer command
    /// handler loads the granter's own <see cref="UserAccount"/> and calls this
    /// method on it to compute <c>GrantRole</c>'s own
    /// <c>granterHasSufficientAuthority</c> parameter.
    /// </summary>
    public bool HoldsRoleAtOrBroaderThan(RoleReference role, OrganizationalScope scope, DateOnly asOfDate) =>
        _roleAssignments.Any(a =>
            a.RevokedOn is null && a.IsEffectiveOn(asOfDate) && a.Role == role && a.Scope.IsBroaderThanOrEqualTo(scope));

    private static bool IsSystemAdministratorAtTenantScope(RoleAssignment assignment) =>
        assignment.Role.Kind == RoleKind.Canonical && assignment.Role.CanonicalRole == CanonicalRole.SystemAdministrator
        && assignment.Scope.Level == ScopeLevel.Tenant;

    private IEnumerable<CanonicalRole?> HeldCanonicalRoles(bool includeRevoked) =>
        _roleAssignments
            .Where(a => includeRevoked || a.RevokedOn is null)
            .Select(a => a.Role.Kind == RoleKind.Canonical ? a.Role.CanonicalRole : null);

    /// <summary>
    /// DOC-012 Section 7's prohibited combinations (AR-003, AR-045): Auditor with
    /// any mutation role (everything except Auditor and the platform's other
    /// read-only role, Executive); SystemAdministrator with HRManager;
    /// PayrollOfficer with HRManager.
    /// </summary>
    internal static bool ViolatesSeparationOfDuties(IEnumerable<CanonicalRole> roles)
    {
        var set = roles.ToHashSet();

        if (set.Contains(CanonicalRole.Auditor) && set.Any(r => r != CanonicalRole.Auditor && r != CanonicalRole.Executive))
        {
            return true;
        }

        if (set.Contains(CanonicalRole.SystemAdministrator) && set.Contains(CanonicalRole.HRManager))
        {
            return true;
        }

        return set.Contains(CanonicalRole.PayrollOfficer) && set.Contains(CanonicalRole.HRManager);
    }
}
