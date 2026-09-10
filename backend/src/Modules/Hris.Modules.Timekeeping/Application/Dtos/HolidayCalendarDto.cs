namespace Hris.Modules.Timekeeping.Application.Dtos;

/// <summary>
/// Read shape for <c>HolidayCalendar</c>. <c>TenantId</c> is nullable because a
/// country-level calendar is platform-provided and has no owning tenant (TK-041).
/// </summary>
public sealed record HolidayCalendarDto(
    Guid Id,
    Guid? TenantId,
    Guid LineageId,
    string Name,
    string Level,
    string ScopeTargetId,
    string CountryCode,
    Guid? ParentCalendarId,
    IReadOnlyList<HolidayDto> Holidays,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status);

public sealed record HolidayDto(Guid Id, DateOnly Date, string Name, string Type, string WorkRule);
