namespace Hris.Modules.Leave.Domain;

/// <summary>Repository contract for the <see cref="LeaveRequest"/> Aggregate Root. Source: docs/04-modules/leave/domain/aggregates.md.</summary>
public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(LeaveRequestId id, CancellationToken cancellationToken);

    /// <summary>Every non-terminal (not Rejected/Cancelled) request for one employee, for LV-031's overlap check.</summary>
    Task<IReadOnlyList<LeaveRequest>> ListNonTerminalByEmployeeAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(LeaveRequest request, CancellationToken cancellationToken);
}
