namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// The six levels logging-framework.md's Log Levels section names, in ascending
/// severity: "Trace, Debug, Information, Warning, Error, Critical." Deliberately the
/// same six names and order as .NET's own <c>Microsoft.Extensions.Logging.LogLevel</c>
/// (minus its sentinel <c>None</c>), since this framework's own Implementation
/// Guidance assumes interoperation with the standard ASP.NET Core logging pipeline
/// rather than a parallel one -- an Infrastructure-layer adapter maps this enum to
/// that one at the point a <see cref="LogEntry"/> is actually emitted.
///
/// A Simple Enumeration per docs/02-architecture/04-domain-driven-design/enumeration-pattern.md:
/// the set is fixed and carries no behavior of its own beyond the ordering
/// <see cref="LogEntry"/> and <see cref="SensitiveDataScrubber"/> compare against
/// (e.g. "is this Error or worse").
/// </summary>
public enum LogSeverity
{
    Trace = 0,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
}
