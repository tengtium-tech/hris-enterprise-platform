using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// The record that a <see cref="UserAccount"/> holds a role, at a scope, for a
/// period, granted by someone, for a stated reason. Source:
/// docs/04-modules/administration/domain/role-assignments.md. A child Entity of
/// <see cref="UserAccount"/>, never an Aggregate Root of its own -- there is no
/// <c>RoleAssignmentRepository</c> (`CTR-ARC-004`), since separation of duties
/// (AR-003) requires the complete assignment set as one consistency boundary.
/// Its constructor and mutators are <c>internal</c>.
/// </summary>
public sealed class RoleAssignment : Entity<RoleAssignmentId>
{
    public RoleReference Role { get; internal set; } = null!;

    public OrganizationalScope Scope { get; internal set; } = null!;

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public Guid GrantedBy { get; }

    public DateTimeOffset GrantedOn { get; }

    public GrantReason Reason { get; }

    public Guid? ApprovalReference { get; }

    public Guid? RevokedBy { get; private set; }

    public DateTimeOffset? RevokedOn { get; private set; }

    public bool IsExpired { get; private set; }

    /// <summary>
    /// Does not accept <see cref="Role"/> or <see cref="Scope"/> as constructor
    /// parameters -- both are owned navigations once mapped, and this project's
    /// own EF Core constructor-binding rule is that no constructor parameter may
    /// bind to an owned navigation (see feedback memory
    /// feedback-ef-core-constructor-binding). Callers assign both via
    /// object-initializer immediately after construction, the identical pattern
    /// <c>EmploymentContract.Period</c> and <c>CompensationRecord.Amount</c>
    /// already establish.
    /// </summary>
    internal RoleAssignment(
        RoleAssignmentId id, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid grantedBy, GrantReason reason,
        Guid? approvalReference, DateTimeOffset grantedOn)
        : base(id)
    {
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        GrantedBy = grantedBy;
        GrantedOn = grantedOn;
        Reason = reason;
        ApprovalReference = approvalReference;
    }

    /// <summary>
    /// AR-022: authorization evaluates assignments in force at the time of
    /// evaluation. A future-dated assignment (<see cref="EffectiveFrom"/> in the
    /// future) is present in the aggregate and absent from this result, per
    /// role-assignments.md's own "Future-Dated Grants" section.
    /// </summary>
    public bool IsEffectiveOn(DateOnly asOfDate) =>
        EffectiveFrom <= asOfDate && (EffectiveTo is null || asOfDate <= EffectiveTo.Value);

    /// <summary>AR-023: revocation end-dates, never deletes.</summary>
    internal void Revoke(DateOnly effectiveDate, Guid revokedBy, DateTimeOffset nowUtc)
    {
        EffectiveTo = effectiveDate;
        RevokedBy = revokedBy;
        RevokedOn = nowUtc;
    }

    /// <summary>
    /// Automatic expiry carries no actor, per role-assignments.md's "Automatic
    /// Expiry" -- marks the assignment so the owning <see cref="UserAccount"/>
    /// raises <see cref="RoleAssignmentExpired"/> at most once per assignment,
    /// even if the scan that discovers past-due assignments runs more than once.
    /// </summary>
    internal void Expire() => IsExpired = true;
}
