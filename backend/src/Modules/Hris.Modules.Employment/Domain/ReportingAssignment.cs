using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents a reporting relationship in effect for a period of time, tracked
/// independently of <see cref="AssignmentHistoryRecord"/>'s position/organizational
/// timeline since a reporting line may change without a position change (matrixed
/// or project-based reporting). Source:
/// docs/04-modules/employment/domain/entities.md, ReportingAssignment. The
/// reporting manager is referenced by the manager's own <see cref="EmploymentId"/>,
/// not <c>EmployeeId</c> -- reporting is a fact about the employment relationship,
/// consistent with employment-assignments.md's own "Reporting Relationship"
/// section. A child Entity of <see cref="EmploymentAssignment"/>, never an
/// Aggregate Root of its own; its constructor is <c>internal</c>.
/// </summary>
public sealed class ReportingAssignment : Entity<ReportingAssignmentId>
{
    public Guid ReportingManagerEmploymentId { get; }

    public DateOnly EffectiveStartDate { get; private set; }

    public DateOnly? EffectiveEndDate { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal ReportingAssignment(
        ReportingAssignmentId id, Guid reportingManagerEmploymentId, DateOnly effectiveStartDate, DateTimeOffset createdAtUtc)
        : base(id)
    {
        ReportingManagerEmploymentId = reportingManagerEmploymentId;
        EffectiveStartDate = effectiveStartDate;
        CreatedAtUtc = createdAtUtc;
    }

    internal void Close(DateOnly endDate)
    {
        EffectiveEndDate = endDate;
    }
}
