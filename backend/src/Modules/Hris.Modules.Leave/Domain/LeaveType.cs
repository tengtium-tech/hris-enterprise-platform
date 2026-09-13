using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// The catalog of leave kinds a tenant can configure policy against: platform-provided
/// statutory types (Service Incentive Leave, Maternity, Paternity, Solo Parent, VAWC,
/// Special Leave Benefit) and freely tenant-defined company types. Source:
/// docs/04-modules/leave/domain/aggregates.md, leave-types.md.
///
/// A statutory type's core identity, citation, and <see cref="StatutoryMinimum"/> are
/// platform-seeded and carry no tenant-facing edit path at all (LV-001); a tenant may
/// only configure a <c>LeavePolicy</c> against it. A statutory type can never be
/// deactivated (LV-004) — it remains available platform-wide as long as the underlying
/// law is in force. Deactivating a tenant-defined type does not retroact against
/// existing <c>LeaveBalance</c> or historical <c>LeaveRequest</c> data (LV-003).
/// </summary>
public sealed class LeaveType : AggregateRoot<LeaveTypeId>
{
    public Guid? TenantId { get; }

    public string Code { get; }

    public string Name { get; }

    public LeaveTypeCategory Category { get; }

    public LeaveTypeScope Scope { get; }

    public string? StatutoryBasis { get; }

    public decimal? StatutoryMinimum { get; }

    public LeaveTypeStatus Status { get; private set; }

    private LeaveType(
        LeaveTypeId id, Guid? tenantId, string code, string name, LeaveTypeCategory category, LeaveTypeScope scope,
        string? statutoryBasis, decimal? statutoryMinimum)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Category = category;
        Scope = scope;
        StatutoryBasis = statutoryBasis;
        StatutoryMinimum = statutoryMinimum;
        Status = LeaveTypeStatus.Active;
    }

    /// <summary>Defines a tenant-owned company leave type (LV-002). Always Active, always Tenant-scoped.</summary>
    public static Result<LeaveType> DefineTenantType(
        LeaveTypeId id, Guid tenantId, string? code, string? name, Guid actorId, DateTimeOffset definedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<LeaveType>(LeaveErrors.LeaveTypeCodeRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LeaveType>(LeaveErrors.LeaveTypeNameRequired);
        }

        var leaveType = new LeaveType(
            id, tenantId, code.Trim(), name.Trim(), LeaveTypeCategory.CompanyDefined, LeaveTypeScope.Tenant, null, null);
        leaveType.AddDomainEvent(new LeaveTypeDefined(Guid.NewGuid(), definedOnUtc, id, tenantId, leaveType.Code, actorId));
        return Result.Success(leaveType);
    }

    /// <summary>
    /// Seeds a platform-provided statutory type (LV-001). Not reachable through any
    /// tenant-facing command — invoked only by the platform's own seed data, which is
    /// why this raises no <see cref="LeaveTypeDefined"/>: that event's own documented
    /// meaning (domain-events.md) is scoped to "a tenant-defined leave type is created."
    /// </summary>
    public static Result<LeaveType> SeedStatutoryType(
        LeaveTypeId id, string? code, string? name, string? statutoryBasis, decimal statutoryMinimum)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<LeaveType>(LeaveErrors.LeaveTypeCodeRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LeaveType>(LeaveErrors.LeaveTypeNameRequired);
        }

        if (string.IsNullOrWhiteSpace(statutoryBasis))
        {
            return Result.Failure<LeaveType>(LeaveErrors.StatutoryBasisRequired);
        }

        if (statutoryMinimum < 0)
        {
            return Result.Failure<LeaveType>(LeaveErrors.StatutoryMinimumMustNotBeNegative);
        }

        var leaveType = new LeaveType(
            id, null, code.Trim(), name.Trim(), LeaveTypeCategory.Statutory, LeaveTypeScope.Platform,
            statutoryBasis.Trim(), statutoryMinimum);
        return Result.Success(leaveType);
    }

    /// <summary>Deactivates a tenant-defined type; never available for a statutory type (LV-004).</summary>
    public Result Deactivate(Guid actorId, string reason, DateTimeOffset deactivatedOnUtc)
    {
        if (Scope == LeaveTypeScope.Platform)
        {
            return Result.Failure(LeaveErrors.CannotDeactivateStatutoryLeaveType);
        }

        if (Status == LeaveTypeStatus.Inactive)
        {
            return Result.Success();
        }

        Status = LeaveTypeStatus.Inactive;
        AddDomainEvent(new LeaveTypeDeactivated(Guid.NewGuid(), deactivatedOnUtc, Id, TenantId!.Value, actorId, reason));
        return Result.Success();
    }
}
