using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents a completed assignment period -- the assigned Position and
/// organizational context that were in force before a transfer. Source:
/// docs/04-modules/employment/domain/entities.md, AssignmentHistoryRecord. A child
/// Entity of <see cref="EmploymentAssignment"/>, never an Aggregate Root of its own;
/// its constructor is <c>internal</c>. Every organizational reference is a plain
/// caller-supplied <see cref="Guid"/>, per this platform's own standing
/// "reference by identifier, never by instance" rule.
/// </summary>
public sealed class AssignmentHistoryRecord : Entity<AssignmentHistoryRecordId>
{
    public Guid PositionId { get; }

    public Guid? DepartmentId { get; }

    public Guid? BusinessUnitId { get; }

    public Guid? CostCenterId { get; }

    public Guid? WorkLocationId { get; }

    public Guid? LegalEntityId { get; }

    public DateOnly EffectiveStartDate { get; }

    public DateOnly EffectiveEndDate { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal AssignmentHistoryRecord(
        AssignmentHistoryRecordId id,
        Guid positionId,
        Guid? departmentId,
        Guid? businessUnitId,
        Guid? costCenterId,
        Guid? workLocationId,
        Guid? legalEntityId,
        DateOnly effectiveStartDate,
        DateOnly effectiveEndDate,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PositionId = positionId;
        DepartmentId = departmentId;
        BusinessUnitId = businessUnitId;
        CostCenterId = costCenterId;
        WorkLocationId = workLocationId;
        LegalEntityId = legalEntityId;
        EffectiveStartDate = effectiveStartDate;
        EffectiveEndDate = effectiveEndDate;
        CreatedAtUtc = createdAtUtc;
    }
}
