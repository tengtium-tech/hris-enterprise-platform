namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// The Domain-owned contract for handing a completed <see cref="LogEntry"/> off for
/// collection, per docs/02-architecture/04-domain-driven-design/repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split.
///
/// Not named <c>ILogEntryRepository</c>: repositories.md's own Repository Principles
/// are about round-trip persistence of Aggregate Roots (load, mutate, save), and
/// <see cref="LogEntry"/> is neither an Aggregate Root nor ever read back through the
/// Domain layer -- "Log Search," "Log Aggregation," and "Monitoring" are explicitly
/// operational/Infrastructure capabilities in logging-framework.md's own Scope
/// section, backed by whatever log platform Infrastructure eventually chooses, not a
/// Domain-layer query surface. This interface is write-only by design.
///
/// No Infrastructure implementation exists yet (backend/README.md: no Sprint 3
/// framework has one). A future implementation adapts this to
/// <c>Microsoft.Extensions.Logging.ILogger</c> (or a structured sink such as Serilog)
/// per this framework's own Implementation Guidance, which assumes interoperation
/// with the standard ASP.NET Core logging pipeline rather than a parallel one.
/// </summary>
public interface ILogSink
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken);
}
