using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// One dated holiday within a <see cref="HolidayCalendar"/>. A child entity, never
/// its own root. Source: docs/04-modules/timekeeping/domain/entities.md.
///
/// aggregates.md's reasoning: a single holiday — one date, one type, one name — has
/// no independent lifecycle or consistency requirement of its own. It is meaningful
/// only as part of the calendar it belongs to, and answering "is this date a
/// holiday" always means resolving the whole layered calendar stack rather than one
/// entry in isolation. This mirrors Administration's <c>PermissionGrant</c> living
/// inside <c>TenantRole</c>.
/// </summary>
public sealed class Holiday : Entity<HolidayId>
{
    public DateOnly Date { get; }

    public string Name { get; }

    public HolidayType Type { get; }

    /// <summary>
    /// Whether a scheduled work day falling on this date still expects attendance.
    /// The premium and pay consequence of each value is payroll's to compute; this
    /// states only the expectation.
    /// </summary>
    public HolidayWorkRule WorkRule { get; }

    internal Holiday(HolidayId id, DateOnly date, string name, HolidayType type, HolidayWorkRule workRule)
        : base(id)
    {
        Date = date;
        Name = name;
        Type = type;
        WorkRule = workRule;
    }

    /// <summary>
    /// The three-way Philippine DOLE statutory split, which TK-043 restricts to
    /// country-level calendars. The remaining values may be used at any level.
    /// </summary>
    internal static bool IsStatutoryType(HolidayType type) =>
        type is HolidayType.RegularHoliday or HolidayType.SpecialNonWorkingHoliday or HolidayType.SpecialWorkingHoliday;
}
