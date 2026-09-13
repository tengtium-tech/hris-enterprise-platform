namespace Hris.Modules.Leave.Domain;

/// <summary>Repository contract for the <see cref="LeaveEncashment"/> Aggregate Root. Source: docs/04-modules/leave/domain/aggregates.md.</summary>
public interface ILeaveEncashmentRepository
{
    Task<LeaveEncashment?> GetByIdAsync(LeaveEncashmentId id, CancellationToken cancellationToken);

    /// <summary>Every PendingApproval encashment against one balance, for LV-072's duplicate check.</summary>
    Task<IReadOnlyList<LeaveEncashment>> ListPendingByBalanceAsync(Guid tenantId, LeaveBalanceId leaveBalanceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveEncashment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(LeaveEncashment encashment, CancellationToken cancellationToken);
}
