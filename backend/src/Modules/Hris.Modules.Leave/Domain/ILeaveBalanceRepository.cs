namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Repository contract for the <see cref="LeaveBalance"/> Aggregate Root. There is
/// deliberately no repository for <see cref="LeaveLedgerEntry"/> — it is reached only
/// through its balance (CTR-ARC-004). Source: docs/04-modules/leave/domain/aggregates.md.
/// </summary>
public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetByIdAsync(LeaveBalanceId id, CancellationToken cancellationToken);

    /// <summary>Unique on (TenantId, EmployeeId, LeaveTypeId) — infrastructure/persistence.md.</summary>
    Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(
        Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId, CancellationToken cancellationToken);

    /// <summary>Every leave-type balance for one employee.</summary>
    Task<IReadOnlyList<LeaveBalance>> ListByEmployeeAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken);
}
