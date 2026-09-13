using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Application;

/// <summary>
/// The one place every handler that loads an aggregate by identifier performs this
/// module's tenant-isolation check, per CTR-ISO-004. Returning not-found for both a
/// genuinely missing record and one belonging to another tenant is deliberate
/// (CTR-ISO-002): a forbidden response would confirm the identifier exists somewhere,
/// which is the enumeration signal the requirement removes.
/// </summary>
internal static class AttendanceLookup
{
    public static async Task<Result<AttendanceRecord>> LoadRecordForTenantAsync(
        IAttendanceRecordRepository repository, Guid recordId, Guid tenantId, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(new AttendanceRecordId(recordId), cancellationToken)
            .ConfigureAwait(false);

        return record is null || record.TenantId != tenantId
            ? Result.Failure<AttendanceRecord>(AttendanceErrors.AttendanceRecordNotFound)
            : Result.Success(record);
    }

    public static async Task<Result<AttendanceAdjustment>> LoadAdjustmentForTenantAsync(
        IAttendanceAdjustmentRepository repository, Guid adjustmentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var adjustment = await repository.GetByIdAsync(new AttendanceAdjustmentId(adjustmentId), cancellationToken)
            .ConfigureAwait(false);

        return adjustment is null || adjustment.TenantId != tenantId
            ? Result.Failure<AttendanceAdjustment>(AttendanceErrors.AttendanceAdjustmentNotFound)
            : Result.Success(adjustment);
    }

    public static async Task<Result<OvertimeRequest>> LoadOvertimeRequestForTenantAsync(
        IOvertimeRequestRepository repository, Guid requestId, Guid tenantId, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(new OvertimeRequestId(requestId), cancellationToken)
            .ConfigureAwait(false);

        return request is null || request.TenantId != tenantId
            ? Result.Failure<OvertimeRequest>(AttendanceErrors.OvertimeRequestNotFound)
            : Result.Success(request);
    }

    public static async Task<Result<AttendancePolicy>> LoadPolicyForTenantAsync(
        IAttendancePolicyRepository repository, Guid policyId, Guid tenantId, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(new AttendancePolicyId(policyId), cancellationToken)
            .ConfigureAwait(false);

        return policy is null || policy.TenantId != tenantId
            ? Result.Failure<AttendancePolicy>(AttendanceErrors.AttendancePolicyNotFound)
            : Result.Success(policy);
    }

    public static async Task<Result<AttendanceDevice>> LoadDeviceForTenantAsync(
        IAttendanceDeviceRepository repository, Guid deviceId, Guid tenantId, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(new AttendanceDeviceId(deviceId), cancellationToken)
            .ConfigureAwait(false);

        return device is null || device.TenantId != tenantId
            ? Result.Failure<AttendanceDevice>(AttendanceErrors.AttendanceDeviceNotFound)
            : Result.Success(device);
    }

    public static async Task<Result<BiometricEnrollment>> LoadEnrollmentForTenantAsync(
        IBiometricEnrollmentRepository repository, Guid enrollmentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(new BiometricEnrollmentId(enrollmentId), cancellationToken)
            .ConfigureAwait(false);

        return enrollment is null || enrollment.TenantId != tenantId
            ? Result.Failure<BiometricEnrollment>(AttendanceErrors.BiometricEnrollmentNotFound)
            : Result.Success(enrollment);
    }
}
