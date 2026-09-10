namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Repository contract for the <see cref="ShiftAssignment"/> Aggregate Root.</summary>
public interface IShiftAssignmentRepository
{
    Task<ShiftAssignment?> GetByIdAsync(ShiftAssignmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ShiftAssignment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Every assignment naming <paramref name="targetId"/> at any level. This is the
    /// candidate set <see cref="ShiftAssignmentResolver"/> consumes, and the reason
    /// resolution can be a pure function: the query gathers, the resolver decides.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListByTargetAsync(
        Guid tenantId, string targetId, CancellationToken cancellationToken);

    /// <summary>
    /// Individual-level assignments for one employee, used for TK-031's overlap check
    /// before a new individual assignment is accepted.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListIndividualByEmployeeAsync(
        Guid tenantId, string employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Assignments whose end date has passed and which have not yet been expired.
    /// Drives TK-032's automatic expiry, which runs without administrative action.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListExpirableAsync(DateOnly asOfDate, CancellationToken cancellationToken);

    Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken);
}
