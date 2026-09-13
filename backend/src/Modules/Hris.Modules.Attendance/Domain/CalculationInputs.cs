namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The outside-world inputs the <see cref="AttendanceCalculationEngine"/> needs for one
/// run, assembled by the <c>RunCalculationCommand</c> handler from Timekeeping's public
/// resolver queries and the effective policy version. Keeping them as a single
/// Attendance-owned value object is what makes the engine pure and reproducible
/// (AT-003): given the same <see cref="CalculationInputs"/> and the same event set, it
/// always produces the same <see cref="CalculationResult"/>, with nothing read live
/// from "today" that could shift the answer.
/// </summary>
public sealed record CalculationInputs(
    Guid? ResolvedWorkShiftId,
    TimeOnly? ShiftStart,
    TimeOnly? ShiftEnd,
    bool ShiftUnresolved,
    bool ShiftAmbiguous,
    bool IsHoliday,
    string? HolidayType,
    IReadOnlyList<OvertimeRequestSummary> ApprovedOvertime,
    PolicyCalculationConfiguration Policy);
