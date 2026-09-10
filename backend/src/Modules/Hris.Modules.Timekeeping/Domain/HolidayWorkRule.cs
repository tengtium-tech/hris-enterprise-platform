namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Whether a scheduled work day falling on a holiday still expects attendance. The
/// premium and pay consequence of each value is computed by payroll; this states
/// only the expectation.
/// </summary>
public enum HolidayWorkRule
{
    NoWorkExpected = 0,
    WorkVoluntary = 1,
    WorkRequired = 2,
}
