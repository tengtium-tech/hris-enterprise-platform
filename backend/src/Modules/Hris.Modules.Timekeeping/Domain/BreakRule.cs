namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// A break within a shift or schedule. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md's BreakPeriod / BreakRule
/// table.
///
/// The two documented names describe the same shape at two grains — a break within a
/// schedule and a break within a shift — so one type serves both rather than
/// duplicating the structure under two names that would drift apart.
///
/// <see cref="Paid"/> is carried but never used to compute anything here. An unpaid
/// break's exclusion from payable hours is <c>attendance</c>'s calculation, per this
/// module's own ownership boundary; Timekeeping states the fact and computes no
/// hours from it.
/// </summary>
/// <param name="Kind">Meal, rest, prayer, or company-defined.</param>
/// <param name="Duration">How long the break lasts.</param>
/// <param name="Window">Where fixed, the window the break occupies; null for a floating break.</param>
/// <param name="Paid">Whether the break is paid time.</param>
/// <param name="Mandatory">Whether the break must be taken.</param>
public sealed record BreakRule(BreakKind Kind, TimeSpan Duration, TimeWindowSnapshot? Window, bool Paid, bool Mandatory);

/// <summary>
/// A plain, serializable start/end pair for a break's fixed window.
///
/// Deliberately not <see cref="TimeWindow"/>: break rules persist as a
/// JSON-serialized collection column, and a <c>ValueObject</c> with a private
/// constructor and a validating factory does not round-trip through a serializer
/// without either a parameterless constructor or bespoke converter support. The
/// validating type stays in use everywhere a window is authored; this record is the
/// storage shape for the collection case only.
/// </summary>
public sealed record TimeWindowSnapshot(TimeOnly Start, TimeOnly End);
