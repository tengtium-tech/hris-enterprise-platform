namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Repository contract for the <see cref="LeaveType"/> Aggregate Root. Source:
/// docs/04-modules/leave/domain/aggregates.md.
/// </summary>
public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(LeaveTypeId id, CancellationToken cancellationToken);

    /// <summary>Every platform-scope (statutory) type plus every type this tenant has defined.</summary>
    Task<IReadOnlyList<LeaveType>> ListCatalogForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken);
}
