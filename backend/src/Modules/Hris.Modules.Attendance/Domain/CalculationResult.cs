namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The terminal output of the <see cref="AttendanceCalculationEngine"/>: the derived
/// <see cref="CalculatedFields"/> (whose <see cref="CalculatedFields.PayableHours"/> is
/// the pipeline's final term, never an entered value), the list of exceptions the
/// earlier steps flagged rather than resolved, and the trigger that caused the run, so
/// every recalculation is auditable (AT-070).
/// </summary>
public sealed record CalculationResult(
    CalculatedFields Fields,
    IReadOnlyList<string> Exceptions,
    string Trigger);
