namespace Hris.Modules.Timekeeping.Domain;

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
