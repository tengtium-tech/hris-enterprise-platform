using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Leave.Application;

/// <summary>
/// The one place every handler that loads an aggregate by identifier performs this
/// module's tenant-isolation check, per CTR-ISO-004 — mirroring
/// <c>Hris.Modules.Attendance.Application.AttendanceLookup</c>'s own reasoning.
/// Returning not-found for both a genuinely missing record and one belonging to
/// another tenant is deliberate (CTR-ISO-002): a forbidden response would confirm the
/// identifier exists somewhere, which is the enumeration signal the requirement
/// removes.
/// </summary>
internal static class LeaveLookup
{
    /// <summary>
    /// A platform-scope (statutory) <see cref="LeaveType"/> has no owning tenant and is
    /// visible to every tenant; a tenant-scope type is visible only to the tenant that
    /// owns it.
    /// </summary>
    public static async Task<Result<LeaveType>> LoadLeaveTypeForTenantAsync(
        ILeaveTypeRepository repository, Guid leaveTypeId, Guid tenantId, CancellationToken cancellationToken)
    {
        var leaveType = await repository.GetByIdAsync(new LeaveTypeId(leaveTypeId), cancellationToken)
            .ConfigureAwait(false);

        return leaveType is null || (leaveType.TenantId is not null && leaveType.TenantId != tenantId)
            ? Result.Failure<LeaveType>(LeaveErrors.LeaveTypeNotFound)
            : Result.Success(leaveType);
    }

    public static async Task<Result<LeavePolicy>> LoadLeavePolicyForTenantAsync(
        ILeavePolicyRepository repository, Guid leavePolicyId, Guid tenantId, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(new LeavePolicyId(leavePolicyId), cancellationToken)
            .ConfigureAwait(false);

        return policy is null || policy.TenantId != tenantId
            ? Result.Failure<LeavePolicy>(LeaveErrors.LeavePolicyNotFound)
            : Result.Success(policy);
    }

    public static async Task<Result<LeaveBalance>> LoadLeaveBalanceForTenantAsync(
        ILeaveBalanceRepository repository, Guid leaveBalanceId, Guid tenantId, CancellationToken cancellationToken)
    {
        var balance = await repository.GetByIdAsync(new LeaveBalanceId(leaveBalanceId), cancellationToken)
            .ConfigureAwait(false);

        return balance is null || balance.TenantId != tenantId
            ? Result.Failure<LeaveBalance>(LeaveErrors.LeaveBalanceNotFound)
            : Result.Success(balance);
    }

    public static async Task<Result<LeaveRequest>> LoadLeaveRequestForTenantAsync(
        ILeaveRequestRepository repository, Guid leaveRequestId, Guid tenantId, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(new LeaveRequestId(leaveRequestId), cancellationToken)
            .ConfigureAwait(false);

        return request is null || request.TenantId != tenantId
            ? Result.Failure<LeaveRequest>(LeaveErrors.LeaveRequestNotFound)
            : Result.Success(request);
    }
}
