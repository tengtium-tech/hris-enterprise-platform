using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// This module's reusable error catalog, per error-pattern.md's "Error Catalog"
/// section, citing the AT-* rule IDs from
/// docs/04-modules/attendance/domain/business-rules.md in each entry's own remarks
/// where one exists.
///
/// Several documented rules have no entry here deliberately, matching every prior
/// module's own documented-gap precedent. AT-003 (calculated results are always
/// reproducible), AT-004 (uniform rounding), AT-010 (one active policy per employee),
/// AT-013 (work date resolved via Timekeeping's anchor rule), AT-041 (rejected
/// overtime never payable), AT-042 (overtime exceeding estimate is flagged) and
/// AT-060 (absence is derived, never recorded) are obligations discharged inside the
/// calculation engine and the aggregate behaviors rather than as standalone error
/// codes a caller surfaces.
/// </summary>
public static class AttendanceErrors
{
    // AttendanceRecord (AT-001, AT-012, AT-030, AT-031, AT-032).
    public static readonly Error AttendanceRecordNotFound = new(
        "Attendance.AttendanceRecordNotFound",
        "The attendance record was not found.",
        ErrorCategory.NotFound);

    public static readonly Error EmployeeIdentifierRequired = new(
        "Attendance.EmployeeIdentifierRequired",
        "An attendance record is always for one employee, identified by a non-empty employee identifier.",
        ErrorCategory.Validation);

    public static readonly Error InvalidStateTransition = new(
        "Attendance.InvalidStateTransition",
        "The requested transition is not permitted in the record's current lifecycle state.",
        ErrorCategory.Domain);

    public static readonly Error AttendanceRecordAlreadyExistsForEmployeeOnDate = new(
        "Attendance.AttendanceRecordAlreadyExistsForEmployeeOnDate",
        "Exactly one attendance record exists per employee per work date unless the tenant configures multiple. (AT-012)",
        ErrorCategory.Conflict);

    public static readonly Error TimeEventIsImmutable = new(
        "Attendance.TimeEventIsImmutable",
        "A captured time event is never edited; correct it through an attendance adjustment. (AT-001)",
        ErrorCategory.Domain);

    public static readonly Error DuplicateTimeEvent = new(
        "Attendance.DuplicateTimeEvent",
        "Two events of the same type from the same source within the dedup window are a duplicate, not a second punch.",
        ErrorCategory.Conflict);

    public static readonly Error RecordNotValidated = new(
        "Attendance.RecordNotValidated",
        "Attendance must be validated before it can be approved. (AT-030)",
        ErrorCategory.Domain);

    public static readonly Error PendingAdjustmentBlocksFinalization = new(
        "Attendance.PendingAdjustmentBlocksFinalization",
        "A record with a pending adjustment cannot be finalized. (AT-031)",
        ErrorCategory.Domain);

    public static readonly Error RecordAlreadyFinalized = new(
        "Attendance.RecordAlreadyFinalized",
        "A finalized record is immutable except through an authorized reopening. (AT-032)",
        ErrorCategory.Domain);

    public static readonly Error RecordNotFinalized = new(
        "Attendance.RecordNotFinalized",
        "The operation requires a finalized record.",
        ErrorCategory.Domain);

    public static readonly Error DeviceNotActive = new(
        "Attendance.DeviceNotActive",
        "Only an Active registered device may submit time events. (AT-050)",
        ErrorCategory.Domain);

    // AttendanceAdjustment (AT-020, AT-021, AT-022, AT-024).
    public static readonly Error AttendanceAdjustmentNotFound = new(
        "Attendance.AttendanceAdjustmentNotFound",
        "The attendance adjustment was not found.",
        ErrorCategory.NotFound);

    public static readonly Error OriginalValueSnapshotRequired = new(
        "Attendance.OriginalValueSnapshotRequired",
        "An adjustment captures the original value as a snapshot at submission, never a live reference. (AT-020)",
        ErrorCategory.Validation);

    public static readonly Error DuplicateAdjustmentRequest = new(
        "Attendance.DuplicateAdjustmentRequest",
        "A duplicate adjustment against the same record and original value is rejected while one is pending. (AT-021)",
        ErrorCategory.Conflict);

    public static readonly Error AdjustmentNotInReviewableState = new(
        "Attendance.AdjustmentNotInReviewableState",
        "The adjustment is not in a state that permits this transition.",
        ErrorCategory.Domain);

    public static readonly Error AdjustmentNotApproved = new(
        "Attendance.AdjustmentNotApproved",
        "Only an approved adjustment may be applied to a record. (AT-024)",
        ErrorCategory.Domain);

    public static readonly Error AdjustmentAlreadyApplied = new(
        "Attendance.AdjustmentAlreadyApplied",
        "The approved adjustment has already been applied to its record.",
        ErrorCategory.Domain);

    // OvertimeRequest (AT-040, AT-041, AT-043).
    public static readonly Error OvertimeRequestNotFound = new(
        "Attendance.OvertimeRequestNotFound",
        "The overtime request was not found.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateOvertimeRequest = new(
        "Attendance.DuplicateOvertimeRequest",
        "A duplicate overtime request for the same employee, work date, and overlapping window is rejected. (AT-043)",
        ErrorCategory.Conflict);

    public static readonly Error OvertimeRequestNotApproved = new(
        "Attendance.OvertimeRequestNotApproved",
        "A rejected or cancelled overtime request is never payable. (AT-041)",
        ErrorCategory.Domain);

    // AttendancePolicy (AT-002, AT-011).
    public static readonly Error AttendancePolicyNotFound = new(
        "Attendance.AttendancePolicyNotFound",
        "The attendance policy was not found.",
        ErrorCategory.NotFound);

    public static readonly Error PolicyAssignmentOverlap = new(
        "Attendance.PolicyAssignmentOverlap",
        "Two assignments of different policies to the same scope target may not overlap. (AT-011)",
        ErrorCategory.Conflict);

    public static readonly Error PolicyAssignmentNotFound = new(
        "Attendance.PolicyAssignmentNotFound",
        "The policy assignment was not found.",
        ErrorCategory.NotFound);

    public static readonly Error PolicyVersionNotDraft = new(
        "Attendance.PolicyVersionNotDraft",
        "A published or superseded policy version is never edited; author a new version. (AT-002)",
        ErrorCategory.Domain);

    public static readonly Error PolicyVersionNotActive = new(
        "Attendance.PolicyVersionNotActive",
        "Only an Active policy version may be revised into a new draft. (AT-002)",
        ErrorCategory.Domain);

    public static readonly Error PolicyRevisionEffectiveDateMustAdvance = new(
        "Attendance.PolicyRevisionEffectiveDateMustAdvance",
        "A revision's effective date must fall after the version it supersedes. (AT-002)",
        ErrorCategory.Validation);

    public static readonly Error PolicyNameRequired = new(
        "Attendance.PolicyNameRequired",
        "An attendance policy requires a name.",
        ErrorCategory.Validation);

    // AttendanceDevice (AT-050, AT-051).
    public static readonly Error AttendanceDeviceNotFound = new(
        "Attendance.AttendanceDeviceNotFound",
        "The attendance device was not found.",
        ErrorCategory.NotFound);

    public static readonly Error DeviceNameRequired = new(
        "Attendance.DeviceNameRequired",
        "An attendance device requires a name.",
        ErrorCategory.Validation);

    public static readonly Error DeviceSerialRequired = new(
        "Attendance.DeviceSerialRequired",
        "An attendance device requires a serial number.",
        ErrorCategory.Validation);

    // BiometricEnrollment (AT-052, AT-053).
    public static readonly Error BiometricEnrollmentNotFound = new(
        "Attendance.BiometricEnrollmentNotFound",
        "The biometric enrollment was not found.",
        ErrorCategory.NotFound);

    public static readonly Error BiometricTemplateReferenceRequired = new(
        "Attendance.BiometricTemplateReferenceRequired",
        "Only an encrypted, non-reversible template reference is stored, never raw biometric data. (AT-052)",
        ErrorCategory.Validation);

    public static readonly Error BiometricConsentRequired = new(
        "Attendance.BiometricConsentRequired",
        "Enrollment requires explicit consent confirmation.",
        ErrorCategory.Validation);

    public static readonly Error BiometricEnrollmentNotActive = new(
        "Attendance.BiometricEnrollmentNotActive",
        "Only an active enrollment may be used for authentication.",
        ErrorCategory.Domain);

    public static readonly Error BiometricRevocationIncomplete = new(
        "Attendance.BiometricRevocationIncomplete",
        "Revocation is not complete until every synchronized device confirms removal. (AT-053)",
        ErrorCategory.Domain);
}
