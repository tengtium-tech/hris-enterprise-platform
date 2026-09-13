namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Downstream payroll processing state of a finalized <see cref="AttendanceRecord"/>.
/// Mirrors the locked-payroll discipline (CTR-PAY-006) one module upstream: once a
/// record reaches <see cref="Processed"/> its values are frozen for payroll.
/// </summary>
public enum PayrollStatus
{
    NotProcessed,
    Processing,
    Processed,
    Locked,
}
