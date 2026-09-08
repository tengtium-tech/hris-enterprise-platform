using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// A single immutable, append-only historical record of a change to an
/// <see cref="Employee"/>'s own current state. Source:
/// docs/04-modules/employee/domain/aggregates.md's own Employee History Aggregate
/// ("Stores immutable historical information... append-only") and
/// employee-history.md's "History Record Structure". A genuine Aggregate Root of
/// its own -- not a child entity of <see cref="Employee"/> -- per aggregates.md's
/// own Aggregate Overview table and Repository Design section ("Repositories exist
/// only for Aggregate Roots... EmployeeHistoryRepository"), unlike Employment's own
/// history entities (<c>EmploymentStatusChange</c> etc.), which that module's own
/// docs model as child entities instead; each module's documented design is
/// followed as written rather than forced into a single shared shape.
///
/// <see cref="EmployeeId"/> is a plain <see cref="Guid"/> even though it references
/// this same module's own <see cref="Employee"/> Aggregate, following this
/// platform's own standing "Aggregates communicate through identifiers, never a
/// direct instance reference" rule (aggregates.md: "No Aggregate should directly
/// reference another Aggregate") -- the identical reasoning already applied to
/// Employment's own cross-aggregate <see cref="Guid"/> references, extended here to
/// a same-module, different-Aggregate-Root case.
///
/// Written directly by this module's own Application-layer command handlers
/// immediately after the corresponding <see cref="Employee"/> aggregate operation
/// succeeds, rather than through a Domain Event subscriber pipeline -- no module in
/// this codebase yet implements <c>IDomainEventSubscriber&lt;TEvent&gt;</c>
/// (declared in Hris.Foundation.Events but unused by any Sprint through Employment),
/// so building one from scratch for this Sprint alone would be new, untested
/// infrastructure rather than following an established pattern. Documented as a
/// scope decision, not a silently dropped requirement.
/// </summary>
public sealed class EmployeeHistory : AggregateRoot<EmployeeHistoryId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public EmployeeHistoryCategory Category { get; }

    public string? PreviousValue { get; }

    public string? NewValue { get; }

    public DateOnly EffectiveDate { get; }

    public string? BusinessReason { get; }

    public Guid? ChangedBy { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    private EmployeeHistory(
        EmployeeHistoryId id, Guid tenantId, Guid employeeId, EmployeeHistoryCategory category, string? previousValue,
        string? newValue, DateOnly effectiveDate, string? businessReason, Guid? changedBy, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Category = category;
        PreviousValue = previousValue;
        NewValue = newValue;
        EffectiveDate = effectiveDate;
        BusinessReason = businessReason;
        ChangedBy = changedBy;
        CreatedAtUtc = createdAtUtc;
    }

    public static EmployeeHistory Record(
        EmployeeHistoryId id, Guid tenantId, Guid employeeId, EmployeeHistoryCategory category, string? previousValue,
        string? newValue, DateOnly effectiveDate, string? businessReason, Guid? changedBy, DateTimeOffset nowUtc) =>
        new(id, tenantId, employeeId, category, previousValue, newValue, effectiveDate, businessReason, changedBy, nowUtc);
}
