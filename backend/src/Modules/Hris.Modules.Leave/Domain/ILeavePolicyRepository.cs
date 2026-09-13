namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Repository contract for the <see cref="LeavePolicy"/> Aggregate Root. There is
/// deliberately no repository for <see cref="PolicyAssignment"/> — it is reached through
/// its policy (CTR-ARC-004). Source: docs/04-modules/leave/domain/aggregates.md.
/// </summary>
public interface ILeavePolicyRepository
{
    Task<LeavePolicy?> GetByIdAsync(LeavePolicyId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeavePolicy>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>Active versions of the given leave type, effective on the work date (LV-011).</summary>
    Task<IReadOnlyList<LeavePolicy>> ListEffectiveForLeaveTypeAsync(
        Guid tenantId, Guid leaveTypeId, DateOnly asOfDate, CancellationToken cancellationToken);

    Task AddAsync(LeavePolicy policy, CancellationToken cancellationToken);
}
