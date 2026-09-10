namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>
/// The answer to "which shift does this employee owe on this date". Carries the
/// unresolved and ambiguous states explicitly rather than returning null for both:
/// a gap in configuration and a conflict in it are different problems with different
/// remedies (TK-030).
/// </summary>
public sealed record ShiftResolutionDto(
    ShiftAssignmentDto? Assignment, Guid? WorkShiftId, bool IsUnresolved, bool IsAmbiguous);

/// <summary>
/// The answer to "is this date a holiday for this scope", including which layer
/// supplied it so the determination is explainable rather than merely asserted.
/// </summary>
public sealed record HolidayResolutionDto(
    HolidayDto? Holiday, Guid? SourceCalendarId, string? SourceLevel, bool IsHoliday);
