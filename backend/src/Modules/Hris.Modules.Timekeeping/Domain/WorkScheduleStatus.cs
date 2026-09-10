namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum WorkScheduleStatus
{
    Draft = 0,
    Active = 1,

    /// <summary>Replaced by a later version. Retained unedited forever (TK-001).</summary>
    Superseded = 2,

    Retired = 3,
}
