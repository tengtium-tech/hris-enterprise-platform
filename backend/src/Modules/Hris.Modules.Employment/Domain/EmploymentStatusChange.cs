using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Records a single change to Operational Status. Source:
/// docs/04-modules/employment/domain/entities.md, EmploymentStatusChange -- "the
/// entity retains its legacy name from before Employment Type, Lifecycle, and
/// Operational Status were split into independent values"; scoped to the
/// Operational Status axis specifically. A child Entity of <see cref="Employment"/>,
/// never an Aggregate Root of its own; its constructor is <c>internal</c>.
/// </summary>
public sealed class EmploymentStatusChange : Entity<EmploymentStatusChangeId>
{
    public OperationalStatus PreviousStatus { get; }

    public OperationalStatus NewStatus { get; }

    public DateOnly EffectiveDate { get; }

    public string Reason { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal EmploymentStatusChange(
        EmploymentStatusChangeId id,
        OperationalStatus previousStatus,
        OperationalStatus newStatus,
        DateOnly effectiveDate,
        string reason,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        EffectiveDate = effectiveDate;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }
}
