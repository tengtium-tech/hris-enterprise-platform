using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section, citing the TK-* rule IDs from
/// docs/04-modules/timekeeping/domain/business-rules.md in each entry's own remarks
/// where one exists.
///
/// Some documented rules have no entry here deliberately, matching every prior
/// module's own documented-gap precedent. TK-003 ("a rule change never retroactively
/// alters already-finalized attendance or payroll") is an obligation on modules that
/// do not exist yet: this module discharges it by never mutating a superseded
/// version and by resolving on the evaluated date, both of which are enforced here,
/// but the finalized-result half cannot be asserted until attendance and payroll
/// exist. TK-050 through TK-052 are Audit Framework concerns rather than this
/// module's own invariants. TK-031's "unless the tenant explicitly configures
/// multi-shift coverage" and TK-034's override authority both depend on tenant
/// policy this module does not own, so each arrives as a caller-supplied signal.
/// </summary>
public static class TimekeepingErrors
{
    // Shared effective-dating and versioning (TK-001, TK-002).
    public static readonly Error SupersededVersionCannotBeModified = new(
        "Timekeeping.SupersededVersionCannotBeModified",
        "A superseded version is never edited; author a new version with its own effective date instead. (TK-001)",
        ErrorCategory.Domain);

    public static readonly Error VersionNotActive = new(
        "Timekeeping.VersionNotActive",
        "The operation requires the currently active version.",
        ErrorCategory.Domain);

    public static readonly Error EffectiveFromNotAfterCurrentVersion = new(
        "Timekeeping.EffectiveFromNotAfterCurrentVersion",
        "A new version takes effect after the version it supersedes, so the two never both apply on the same date. (TK-002)",
        ErrorCategory.Validation);

    // Work schedule (TK-010 through TK-012).
    public static readonly Error WorkScheduleNotFound = new(
        "Timekeeping.WorkScheduleNotFound",
        "The work schedule was not found.",
        ErrorCategory.NotFound);

    public static readonly Error WorkScheduleNameRequired = new(
        "Timekeeping.WorkScheduleNameRequired",
        "A work schedule requires a name.",
        ErrorCategory.Validation);

    public static readonly Error WorkingDayPatternRequiresAWorkingDay = new(
        "Timekeeping.WorkingDayPatternRequiresAWorkingDay",
        "A schedule with zero working days is not a schedule. (TK-010)",
        ErrorCategory.Validation);

    public static readonly Error WorkingDayPatternMustPartitionTheWeek = new(
        "Timekeeping.WorkingDayPatternMustPartitionTheWeek",
        "Working days and rest days partition the week completely; no day may be both, and none may be neither.",
        ErrorCategory.Validation);

    public static readonly Error ScheduleAssignmentOverlapsExisting = new(
        "Timekeeping.ScheduleAssignmentOverlapsExisting",
        "Two assignments of this schedule to the same organizational unit may not have overlapping effective periods. (TK-011)",
        ErrorCategory.Conflict);

    public static readonly Error ScheduleAssignmentNotFound = new(
        "Timekeeping.ScheduleAssignmentNotFound",
        "The schedule assignment is not part of this schedule.",
        ErrorCategory.NotFound);

    public static readonly Error AssignmentTargetRequired = new(
        "Timekeeping.AssignmentTargetRequired",
        "An assignment requires the identifier of the target it applies to.",
        ErrorCategory.Validation);

    // Work shift (TK-020 through TK-023).
    public static readonly Error WorkShiftNotFound = new(
        "Timekeeping.WorkShiftNotFound",
        "The work shift was not found.",
        ErrorCategory.NotFound);

    public static readonly Error WorkShiftNameRequired = new(
        "Timekeeping.WorkShiftNameRequired",
        "A work shift requires a name.",
        ErrorCategory.Validation);

    public static readonly Error ShiftCodeRequired = new(
        "Timekeeping.ShiftCodeRequired",
        "A work shift requires a short code.",
        ErrorCategory.Validation);

    public static readonly Error ShiftCodeNotUniqueWithinTenant = new(
        "Timekeeping.ShiftCodeNotUniqueWithinTenant",
        "A shift code is unique within its tenant.",
        ErrorCategory.Conflict);

    public static readonly Error OvernightShiftRequiresAnchorRule = new(
        "Timekeeping.OvernightShiftRequiresAnchorRule",
        "An overnight shift states its work-date anchor rule explicitly; it is never left for a consumer to infer. (TK-020)",
        ErrorCategory.Validation);

    public static readonly Error AnchorRuleProhibitedForNonOvernightShift = new(
        "Timekeeping.AnchorRuleProhibitedForNonOvernightShift",
        "A non-overnight shift carries no anchor rule; a rule for a case that cannot occur invites a consumer to apply it wrongly. (TK-020)",
        ErrorCategory.Validation);

    public static readonly Error SplitShiftPeriodsOverlap = new(
        "Timekeeping.SplitShiftPeriodsOverlap",
        "Periods within one shift do not overlap each other. (TK-021)",
        ErrorCategory.Validation);

    public static readonly Error SplitShiftRequiresAtLeastTwoPeriods = new(
        "Timekeeping.SplitShiftRequiresAtLeastTwoPeriods",
        "A split shift describes more than one working period; a single period is an ordinary shift.",
        ErrorCategory.Validation);

    // Timing value objects.
    public static readonly Error TimeWindowEndNotAfterStart = new(
        "Timekeeping.TimeWindowEndNotAfterStart",
        "A time window's end is after its start unless the window is explicitly anchored as crossing midnight.",
        ErrorCategory.Validation);

    public static readonly Error FixedTimingRequiresWindow = new(
        "Timekeeping.FixedTimingRequiresWindow",
        "A fixed shift carries a fixed window and no flexible fields.",
        ErrorCategory.Validation);

    public static readonly Error FlexibleTimingRequiresAllFlexibleFields = new(
        "Timekeeping.FlexibleTimingRequiresAllFlexibleFields",
        "A flexible shift carries an earliest start, a latest start, core hours, and required hours, and no fixed window.",
        ErrorCategory.Validation);

    public static readonly Error FlexibleTimingEarliestNotBeforeLatest = new(
        "Timekeeping.FlexibleTimingEarliestNotBeforeLatest",
        "A flexible shift's earliest start is before its latest start.",
        ErrorCategory.Validation);

    public static readonly Error FlexibleTimingCoreHoursOutsideRange = new(
        "Timekeeping.FlexibleTimingCoreHoursOutsideRange",
        "A flexible shift's core hours fall within the range its earliest start and required hours permit.",
        ErrorCategory.Validation);

    public static readonly Error RequiredHoursMustBePositive = new(
        "Timekeeping.RequiredHoursMustBePositive",
        "A flexible shift's required hours must be positive.",
        ErrorCategory.Validation);

    public static readonly Error ShiftPeriodEndNotAfterStart = new(
        "Timekeeping.ShiftPeriodEndNotAfterStart",
        "A shift period's end is after its start.",
        ErrorCategory.Validation);

    public static readonly Error MandatoryBreakRequiresPositiveDuration = new(
        "Timekeeping.MandatoryBreakRequiresPositiveDuration",
        "A mandatory break's duration is greater than zero.",
        ErrorCategory.Validation);

    // Shift assignment (TK-030 through TK-036).
    public static readonly Error ShiftAssignmentNotFound = new(
        "Timekeeping.ShiftAssignmentNotFound",
        "The shift assignment was not found.",
        ErrorCategory.NotFound);

    public static readonly Error TemporaryAssignmentRequiresEndDate = new(
        "Timekeeping.TemporaryAssignmentRequiresEndDate",
        "A temporary assignment carries a required end date and expires without administrative action. (TK-032)",
        ErrorCategory.Validation);

    public static readonly Error IndividualAssignmentRequiresEmployeeTarget = new(
        "Timekeeping.IndividualAssignmentRequiresEmployeeTarget",
        "An individual-level assignment targets an employee; every other level targets an organizational unit or classification.",
        ErrorCategory.Validation);

    public static readonly Error OverlappingIndividualAssignment = new(
        "Timekeeping.OverlappingIndividualAssignment",
        "An employee holds no second individual-level assignment overlapping this one unless the tenant configures multi-shift coverage. (TK-031)",
        ErrorCategory.Conflict);

    public static readonly Error AssignmentNotActiveOrScheduled = new(
        "Timekeeping.AssignmentNotActiveOrScheduled",
        "Only a scheduled or active assignment may be swapped, expired, or cancelled.",
        ErrorCategory.Domain);

    public static readonly Error SwapRequiresTwoDistinctAssignments = new(
        "Timekeeping.SwapRequiresTwoDistinctAssignments",
        "A swap links two different assignments; an assignment is never swapped with itself. (TK-033)",
        ErrorCategory.Validation);

    public static readonly Error SwapRequiresBothPartiesConsent = new(
        "Timekeeping.SwapRequiresBothPartiesConsent",
        "A swap does not take effect until both parties consent, or the acting user holds override authority. (TK-034)",
        ErrorCategory.Domain);

    public static readonly Error AssignmentAlreadyExpired = new(
        "Timekeeping.AssignmentAlreadyExpired",
        "The assignment has already ended.",
        ErrorCategory.Domain);

    // Assignment resolution (TK-030).
    public static readonly Error NoApplicableAssignment = new(
        "Timekeeping.NoApplicableAssignment",
        "No shift assignment applies to this employee on this date. (TK-030)",
        ErrorCategory.NotFound);

    public static readonly Error AmbiguousAssignmentAtSameLevel = new(
        "Timekeeping.AmbiguousAssignmentAtSameLevel",
        "Two assignments at the same precedence level apply to this employee on this date; resolution refuses to pick silently. (TK-030)",
        ErrorCategory.Conflict);

    // Holiday calendar (TK-040 through TK-044).
    public static readonly Error HolidayCalendarNotFound = new(
        "Timekeeping.HolidayCalendarNotFound",
        "The holiday calendar was not found.",
        ErrorCategory.NotFound);

    public static readonly Error HolidayCalendarNameRequired = new(
        "Timekeeping.HolidayCalendarNameRequired",
        "A holiday calendar requires a name.",
        ErrorCategory.Validation);

    public static readonly Error HolidayCalendarScopeRequired = new(
        "Timekeeping.HolidayCalendarScopeRequired",
        "A holiday calendar requires the scope it applies to.",
        ErrorCategory.Validation);

    public static readonly Error HolidayNameRequired = new(
        "Timekeeping.HolidayNameRequired",
        "A holiday requires a name.",
        ErrorCategory.Validation);

    public static readonly Error HolidayNotFound = new(
        "Timekeeping.HolidayNotFound",
        "The holiday is not part of this calendar.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateHolidayDate = new(
        "Timekeeping.DuplicateHolidayDate",
        "The calendar already defines a holiday on this date; revise the existing entry rather than adding a second.",
        ErrorCategory.Conflict);

    public static readonly Error CountryLayerIsReadOnlyToTenant = new(
        "Timekeeping.CountryLayerIsReadOnlyToTenant",
        "The platform-provided country holiday layer is statutory data, not tenant configuration; add company holidays on top instead. (TK-041)",
        ErrorCategory.Domain);

    public static readonly Error StatutoryHolidayTypeRequiresCountryLayer = new(
        "Timekeeping.StatutoryHolidayTypeRequiresCountryLayer",
        "A statutory holiday classification may be used only within a country-level calendar. (TK-043)",
        ErrorCategory.Validation);

    public static readonly Error ParentCalendarRequiredForNonCountryLayer = new(
        "Timekeeping.ParentCalendarRequiredForNonCountryLayer",
        "A region or company calendar layers on top of a parent calendar. (TK-042)",
        ErrorCategory.Validation);

    public static readonly Error ParentCalendarProhibitedForCountryLayer = new(
        "Timekeeping.ParentCalendarProhibitedForCountryLayer",
        "A country-level calendar is the base layer and has no parent.",
        ErrorCategory.Validation);

    public static readonly Error CalendarNotDraft = new(
        "Timekeeping.CalendarNotDraft",
        "Only a draft calendar may be edited; a published calendar is revised by superseding it. (TK-044)",
        ErrorCategory.Domain);

    public static readonly Error CalendarNotPublished = new(
        "Timekeeping.CalendarNotPublished",
        "The operation requires a published calendar.",
        ErrorCategory.Domain);

    public static readonly Error CalendarHasNoHolidays = new(
        "Timekeeping.CalendarHasNoHolidays",
        "A calendar with no holiday entries has nothing to publish.",
        ErrorCategory.Domain);
}
