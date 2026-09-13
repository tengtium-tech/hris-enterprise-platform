namespace Hris.Modules.Leave.Domain;

/// <summary>Repository contract for the <see cref="LeaveAdjustment"/> Aggregate Root. Source: docs/04-modules/leave/domain/aggregates.md.</summary>
public interface ILeaveAdjustmentRepository
{
    Task<LeaveAdjustment?> GetByIdAsync(LeaveAdjustmentId id, CancellationToken cancellationToken);

    /// <summary>Every non-terminal (not Rejected/Cancelled/Applied) adjustment against one balance, for LV-053's duplicate check.</summary>
    Task<IReadOnlyList<LeaveAdjustment>> ListPendingByBalanceAsync(Guid tenantId, LeaveBalanceId leaveBalanceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveAdjustment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(LeaveAdjustment adjustment, CancellationToken cancellationToken);
}
