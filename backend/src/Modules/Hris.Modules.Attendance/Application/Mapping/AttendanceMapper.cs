using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Domain;

namespace Hris.Modules.Attendance.Application.Mapping;

/// <summary>
/// Explicit hand-written projection from aggregate to DTO, per mapping.md's convention
/// across every prior module: no reflection-based mapper, so what leaves the module is
/// visible in source and a newly added domain property never escapes into a response by
/// default. No mapper here handles <c>BiometricTemplateReference</c> — the only biometric
/// data that ever crosses this boundary is the pointer, and even that is intentionally
/// withheld from <see cref="BiometricEnrollmentStatusDto"/> (dto-design.md).
/// </summary>
internal static class AttendanceMapper
{
    public static AttendanceRecordDto ToDto(AttendanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new AttendanceRecordDto(
            record.Id.Value,
            record.TenantId,
            record.EmployeeId,
            record.WorkDate,
            record.Status.ToString(),
            record.ApprovalStatus.ToString(),
            record.PayrollStatus.ToString(),
            record.WorkShiftId,
            record.HolidayCalendarId,
            record.Calculated is null ? null : ToDto(record.Calculated),
            record.Exceptions.ToList(),
            record.PendingAdjustmentCount,
            record.TimeEvents.Select(ToDto).ToList());
    }

    public static AttendanceRecordSummaryDto ToSummary(AttendanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new AttendanceRecordSummaryDto(
            record.Id.Value,
            record.TenantId,
            record.EmployeeId,
            record.WorkDate,
            record.Status.ToString(),
            record.ApprovalStatus.ToString(),
            record.PayrollStatus.ToString(),
            record.WorkShiftId,
            record.HolidayCalendarId,
            record.Calculated?.WorkingHours,
            record.Calculated?.PayableHours,
            record.Calculated?.OvertimeHours,
            record.PendingAdjustmentCount);
    }

    public static CalculatedFieldsDto ToDto(CalculatedFields fields) =>
        new(
            fields.WorkingHours,
            fields.PayableHours,
            fields.OvertimeHours,
            fields.LateMinutes,
            fields.UndertimeMinutes,
            fields.HolidayHours,
            fields.NightDifferentialHours);

    public static TimeEventDto ToDto(TimeEvent timeEvent) =>
        new(
            timeEvent.Id.Value,
            timeEvent.EventType.ToString(),
            timeEvent.TimestampUtc,
            timeEvent.Source.ToString(),
            timeEvent.AttendanceDeviceId,
            timeEvent.RawValue,
            timeEvent.OriginatingTimeZone);

    public static AttendanceAdjustmentDto ToDto(AttendanceAdjustment adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);

        return new AttendanceAdjustmentDto(
            adjustment.Id.Value,
            adjustment.TenantId,
            adjustment.AttendanceRecordId.Value,
            adjustment.WorkDate,
            adjustment.Field,
            adjustment.OriginalValue,
            adjustment.RequestedValue,
            adjustment.Category.ToString(),
            adjustment.Reason,
            adjustment.SupportingDocuments.ToList(),
            adjustment.SubmittedBy,
            adjustment.SubmittedOn,
            adjustment.Status.ToString(),
            adjustment.ReviewNotes,
            adjustment.ReviewerId,
            adjustment.ApproverId,
            adjustment.Decision is null ? null : ToDto(adjustment.Decision),
            adjustment.ApprovedOn);
    }

    public static ApprovalDecisionDto ToDto(ApprovalDecision decision) =>
        new(
            decision.ApproverId,
            decision.Outcome.ToString(),
            decision.DecidedOnUtc,
            decision.Comments,
            decision.DelegateId,
            decision.OriginalApproverId);

    public static OvertimeRequestDto ToDto(OvertimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new OvertimeRequestDto(
            request.Id.Value,
            request.TenantId,
            request.EmployeeId,
            request.WorkDate,
            request.PlannedStart,
            request.PlannedEnd,
            request.EstimatedHours,
            request.Category.ToString(),
            request.Justification,
            request.Status.ToString(),
            request.ApproverId,
            request.DecidedOn,
            request.RejectionReason);
    }

    public static AttendancePolicyDto ToDto(AttendancePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var configuration = policy.Configuration;

        return new AttendancePolicyDto(
            policy.Id.Value,
            policy.TenantId,
            policy.LineageId,
            policy.Name,
            policy.Version,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            policy.Status.ToString(),
            policy.CreatedBy,
            policy.CreatedOn,
            ToDto(configuration.WorkingHours),
            new GracePeriodDto(configuration.GracePeriod.Minutes),
            configuration.RoundingRule.ToString(),
            ToDto(configuration.BreakPolicy),
            ToDto(configuration.OvertimePolicy),
            ToDto(configuration.HolidayRestDay),
            policy.PolicyAssignments.Select(ToDto).ToList());
    }

    public static WorkingHoursConfigurationDto ToDto(WorkingHoursConfiguration configuration) =>
        new(
            configuration.StandardDailyHours,
            configuration.MaximumDailyHours,
            configuration.MinimumDailyHours,
            configuration.StandardWeeklyHours,
            configuration.CoreHoursStart,
            configuration.CoreHoursEnd);

    public static BreakPolicyConfigurationDto ToDto(BreakPolicyConfiguration configuration) =>
        new(
            configuration.RequiresBreak,
            configuration.MinimumDurationMinutes,
            configuration.MaximumDurationMinutes,
            configuration.BreaksPaid);

    public static OvertimePolicyConfigurationDto ToDto(OvertimePolicyConfiguration configuration) =>
        new(
            configuration.Eligible,
            configuration.RequiresPriorAuthorization,
            configuration.DailyThresholdHours,
            configuration.WeeklyThresholdHours);

    public static HolidayRestDayConfigurationDto ToDto(HolidayRestDayConfiguration configuration) =>
        new(configuration.HolidayPremiumApplies, configuration.RestDayPremiumApplies);

    public static PolicyAssignmentDto ToDto(PolicyAssignment assignment) =>
        new(
            assignment.Id.Value,
            assignment.ScopeLevel.ToString(),
            assignment.ScopeTargetId,
            assignment.EffectiveFrom,
            assignment.EffectiveTo,
            assignment.AssignedBy,
            assignment.AssignedOn);

    public static AttendanceDeviceDto ToDto(AttendanceDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var configuration = device.Configuration;

        return new AttendanceDeviceDto(
            device.Id.Value,
            device.TenantId,
            device.Name,
            device.SerialNumber,
            device.Manufacturer,
            device.Model,
            device.FirmwareVersion,
            device.Type.ToString(),
            device.Location.ReferenceId,
            new DeviceConfigurationDto(configuration.TimeZoneId, configuration.HeartbeatIntervalSeconds, configuration.AutoOfflineDetection),
            device.Status.ToString(),
            device.LastSynchronizedUtc);
    }

    public static BiometricEnrollmentStatusDto ToDto(BiometricEnrollment enrollment)
    {
        ArgumentNullException.ThrowIfNull(enrollment);

        return new BiometricEnrollmentStatusDto(
            enrollment.Id.Value,
            enrollment.TenantId,
            enrollment.EmployeeId,
            enrollment.Method.ToString(),
            enrollment.Status.ToString(),
            enrollment.Vendor,
            enrollment.SynchronizedDeviceIds.ToList());
    }
}
