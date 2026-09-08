using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Aggregate Root managing where and how an Employment is deployed within the
/// organization -- the third layer of ADR-0008's own ownership model, separate from
/// <see cref="Employment"/> because the two change on entirely different triggers
/// (employment-assignments.md's own comparison table: regularization changes
/// Employment Type but not Assignment; a transfer changes Assignment but not
/// Employment Type). This single Aggregate instance represents the CURRENT
/// deployment for its Employment; every change closes the current state into an
/// immutable <see cref="AssignmentHistoryRecord"/> and updates the same instance's
/// own current fields, rather than creating a new Aggregate instance per period --
/// consistent with "An Employment cannot exist without at least one Employment
/// Assignment" (ASG-002) describing one ongoing Assignment record per Employment,
/// not one per period.
///
/// Every organizational and Position reference is a plain caller-supplied
/// <see cref="Guid"/>, per this platform's own standing "reference by identifier,
/// never by instance" rule -- <see cref="AssignmentRequiresActivePosition"/>-shaped
/// booleans (ASG-001) are computed by the calling Application-layer command handler
/// against the Position module before this Aggregate's methods are called, the
/// identical pattern Position module's own <c>AssignReportingPosition</c> already
/// establishes for a comparable cross-module fact.
///
/// Promotion and demotion classification (<see cref="MovementType"/>) is
/// caller-supplied rather than derived from a live Job Grade comparison -- see
/// <see cref="Domain.MovementType"/>'s own remarks for why this Aggregate cannot
/// compute it itself.
/// </summary>
public sealed class EmploymentAssignment : AggregateRoot<EmploymentAssignmentId>
{
    private readonly List<AssignmentHistoryRecord> _history = [];
    private readonly List<ReportingAssignment> _reportingHistory = [];

    public Guid TenantId { get; }

    public Guid EmploymentId { get; }

    public Guid PositionId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public Guid? BusinessUnitId { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public Guid? WorkLocationId { get; private set; }

    public Guid? LegalEntityId { get; private set; }

    public WorkArrangement WorkArrangement { get; private set; }

    public Guid? ReportingManagerEmploymentId { get; private set; }

    public DateOnly EffectiveStartDate { get; private set; }

    public bool IsEnded { get; private set; }

    public DateOnly? EndedDate { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<AssignmentHistoryRecord> History => _history.AsReadOnly();

    public IReadOnlyList<ReportingAssignment> ReportingHistory => _reportingHistory.AsReadOnly();

    private EmploymentAssignment(
        EmploymentAssignmentId id, Guid tenantId, Guid employmentId, Guid positionId, Guid? departmentId,
        Guid? businessUnitId, Guid? costCenterId, Guid? workLocationId, Guid? legalEntityId,
        WorkArrangement workArrangement, Guid? reportingManagerEmploymentId, DateOnly effectiveStartDate,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        EmploymentId = employmentId;
        PositionId = positionId;
        DepartmentId = departmentId;
        BusinessUnitId = businessUnitId;
        CostCenterId = costCenterId;
        WorkLocationId = workLocationId;
        LegalEntityId = legalEntityId;
        WorkArrangement = workArrangement;
        ReportingManagerEmploymentId = reportingManagerEmploymentId;
        EffectiveStartDate = effectiveStartDate;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates the Employment Assignment. <paramref name="positionIsActive"/> is
    /// computed by the calling Application-layer command handler (ASG-001).
    /// </summary>
    public static Result<EmploymentAssignment> Create(
        EmploymentAssignmentId id, Guid tenantId, Guid employmentId, Guid positionId, Guid? departmentId,
        Guid? businessUnitId, Guid? costCenterId, Guid? workLocationId, Guid? legalEntityId,
        WorkArrangement workArrangement, Guid? reportingManagerEmploymentId, DateOnly effectiveStartDate,
        bool positionIsActive, DateTimeOffset nowUtc)
    {
        if (!positionIsActive)
        {
            return Result.Failure<EmploymentAssignment>(EmploymentErrors.AssignmentRequiresActivePosition);
        }

        if (reportingManagerEmploymentId == employmentId)
        {
            return Result.Failure<EmploymentAssignment>(EmploymentErrors.SelfReportingProhibited);
        }

        var assignment = new EmploymentAssignment(
            id, tenantId, employmentId, positionId, departmentId, businessUnitId, costCenterId, workLocationId,
            legalEntityId, workArrangement, reportingManagerEmploymentId, effectiveStartDate, nowUtc);

        if (reportingManagerEmploymentId.HasValue)
        {
            assignment._reportingHistory.Add(new ReportingAssignment(
                new ReportingAssignmentId(Guid.NewGuid()), reportingManagerEmploymentId.Value, effectiveStartDate, nowUtc));
        }

        assignment.AddDomainEvent(new EmploymentAssignmentCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, employmentId, positionId, effectiveStartDate));
        return Result.Success(assignment);
    }

    /// <summary>
    /// Changes the assigned Position, organizational context, and Work Arrangement,
    /// archiving the current state as an immutable <see cref="AssignmentHistoryRecord"/>.
    /// Always raises <see cref="EmploymentAssignmentChanged"/>; additionally raises
    /// <see cref="EmploymentPromoted"/>, <see cref="EmploymentDemoted"/>, or
    /// <see cref="EmploymentTransferred"/> depending on <paramref name="movementType"/>
    /// (employment-assignments.md's own "Relationship to movement events").
    /// <paramref name="positionIsActive"/> is computed by the calling
    /// Application-layer command handler (ASG-001).
    /// </summary>
    public Result ChangePosition(
        Guid newPositionId, Guid? newDepartmentId, Guid? newBusinessUnitId, Guid? newCostCenterId,
        Guid? newWorkLocationId, Guid? newLegalEntityId, WorkArrangement newWorkArrangement, MovementType movementType,
        DateOnly effectiveDate, string? approvalReference, bool positionIsActive, DateTimeOffset nowUtc)
    {
        if (IsEnded)
        {
            return Result.Failure(EmploymentErrors.AssignmentAlreadyEnded);
        }

        if (!positionIsActive)
        {
            return Result.Failure(EmploymentErrors.AssignmentRequiresActivePosition);
        }

        var previousPositionId = PositionId;
        var previousOrganizationalUnitId = DepartmentId;

        _history.Add(new AssignmentHistoryRecord(
            new AssignmentHistoryRecordId(Guid.NewGuid()), PositionId, DepartmentId, BusinessUnitId, CostCenterId,
            WorkLocationId, LegalEntityId, EffectiveStartDate, effectiveDate, nowUtc));

        PositionId = newPositionId;
        DepartmentId = newDepartmentId;
        BusinessUnitId = newBusinessUnitId;
        CostCenterId = newCostCenterId;
        WorkLocationId = newWorkLocationId;
        LegalEntityId = newLegalEntityId;
        WorkArrangement = newWorkArrangement;
        EffectiveStartDate = effectiveDate;

        AddDomainEvent(new EmploymentAssignmentChanged(
            Guid.NewGuid(), nowUtc, Id, EmploymentId, previousPositionId, newPositionId, effectiveDate));

        switch (movementType)
        {
            case MovementType.Promotion:
                AddDomainEvent(new EmploymentPromoted(
                    Guid.NewGuid(), nowUtc, EmploymentId, previousPositionId, newPositionId, effectiveDate));
                break;
            case MovementType.Demotion:
                AddDomainEvent(new EmploymentDemoted(
                    Guid.NewGuid(), nowUtc, EmploymentId, previousPositionId, newPositionId, effectiveDate));
                break;
            case MovementType.Lateral:
            default:
                if (previousOrganizationalUnitId != newDepartmentId)
                {
                    AddDomainEvent(new EmploymentTransferred(
                        Guid.NewGuid(), nowUtc, EmploymentId, previousOrganizationalUnitId, newDepartmentId, effectiveDate));
                }

                break;
        }

        return Result.Success();
    }

    /// <summary>
    /// Changes the reporting manager, tracked on its own timeline independent of
    /// Position/organizational-unit history. <paramref name="wouldCreateCircularReporting"/>
    /// is computed by the calling Application-layer command handler (ASG-006).
    /// </summary>
    public Result ChangeReportingManager(
        Guid newReportingManagerEmploymentId, bool wouldCreateCircularReporting, DateOnly effectiveDate, DateTimeOffset nowUtc)
    {
        if (IsEnded)
        {
            return Result.Failure(EmploymentErrors.AssignmentAlreadyEnded);
        }

        if (newReportingManagerEmploymentId == EmploymentId)
        {
            return Result.Failure(EmploymentErrors.SelfReportingProhibited);
        }

        if (wouldCreateCircularReporting)
        {
            return Result.Failure(EmploymentErrors.CircularReportingProhibited);
        }

        var previous = ReportingManagerEmploymentId;
        var current = _reportingHistory.SingleOrDefault(r => r.EffectiveEndDate is null);
        current?.Close(effectiveDate);

        _reportingHistory.Add(new ReportingAssignment(
            new ReportingAssignmentId(Guid.NewGuid()), newReportingManagerEmploymentId, effectiveDate, nowUtc));
        ReportingManagerEmploymentId = newReportingManagerEmploymentId;

        AddDomainEvent(new ReportingManagerChanged(
            Guid.NewGuid(), nowUtc, Id, EmploymentId, previous, newReportingManagerEmploymentId));
        return Result.Success();
    }

    public Result End(DateOnly effectiveDate, DateTimeOffset nowUtc)
    {
        if (IsEnded)
        {
            return Result.Failure(EmploymentErrors.AssignmentAlreadyEnded);
        }

        _history.Add(new AssignmentHistoryRecord(
            new AssignmentHistoryRecordId(Guid.NewGuid()), PositionId, DepartmentId, BusinessUnitId, CostCenterId,
            WorkLocationId, LegalEntityId, EffectiveStartDate, effectiveDate, nowUtc));

        IsEnded = true;
        EndedDate = effectiveDate;
        AddDomainEvent(new EmploymentAssignmentEnded(Guid.NewGuid(), nowUtc, Id, EmploymentId, effectiveDate));
        return Result.Success();
    }
}
