namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Which end of an overnight shift determines the single work date its hours belong
/// to. The platform default is <see cref="ShiftStart"/> — a 22:00 to 06:00 shift
/// belongs to the date it starts on.
/// </summary>
public enum WorkDateAnchorPoint
{
    ShiftStart = 0,
    ShiftEnd = 1,
}
