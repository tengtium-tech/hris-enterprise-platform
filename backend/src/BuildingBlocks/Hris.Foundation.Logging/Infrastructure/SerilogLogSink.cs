using Hris.Foundation.Logging.Domain;
using Hris.SharedKernel;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Hris.Foundation.Logging.Infrastructure;

/// <summary>
/// The Infrastructure-layer adapter <see cref="ILogSink"/>'s own remarks call for --
/// "A future implementation adapts this to Microsoft.Extensions.Logging.ILogger (or a
/// structured sink such as Serilog)." Serilog specifically, not the generic
/// <c>Microsoft.Extensions.Logging.ILogger</c> abstraction, because
/// technology-stack.md's Monitoring &amp; Observability table names Serilog directly
/// as this platform's own binding logging technology (not an illustrative example the
/// way the CI pipeline's original vulnerability-scanner placeholder was) --
/// <c>Hris.Foundation.Logging.Domain</c> stays free of any logging-library reference,
/// per Clean Architecture's inward dependency rule; this is the one place that
/// reference is allowed.
///
/// Every <see cref="LogEntry"/> field structured-logging.md's own Structured Logging
/// section names -- Service, Operation/Module, Correlation, Tenant, User, Exception,
/// Metadata -- is attached as a named Serilog property via <c>ForContext</c>, not
/// interpolated into the message text, so log sinks and search tooling downstream can
/// query on them individually rather than parsing free text (logging-framework.md's
/// own Log Search section: "Search should support filtering... by Correlation ID,
/// Tenant, User, Service, Module").
/// </summary>
internal sealed class SerilogLogSink : ILogSink
{
    private readonly ILogger _logger;

    public SerilogLogSink(ILogger logger)
    {
        _logger = Guard.AgainstNull(logger, nameof(logger));
    }

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(entry, nameof(entry));

        cancellationToken.ThrowIfCancellationRequested();

        var contextualLogger = _logger
            .ForContext("Service", entry.Context.Service)
            .ForContext("Module", entry.Context.Module)
            .ForContext("CorrelationId", entry.Context.CorrelationId.ToString())
            .ForContext("TenantId", entry.Context.TenantId)
            .ForContext("UserId", entry.Context.UserId)
            .ForContext("LogEntryId", entry.Id);

        foreach (var (key, value) in entry.Metadata)
        {
            contextualLogger = contextualLogger.ForContext(key, value);
        }

        var level = ToSerilogLevel(entry.Severity);

        if (entry.ExceptionDetails is { Length: > 0 } exceptionDetails)
        {
            contextualLogger.Write(level, "{Message} | {ExceptionDetails}", entry.Message, exceptionDetails);
        }
        else
        {
            contextualLogger.Write(level, "{Message}", entry.Message);
        }

        // Serilog's own Write call is synchronous (in-process buffering to its
        // configured sinks); this method stays Task-returning only because
        // ILogSink's Domain-owned contract declares it that way for future sinks
        // (e.g. a network log-collector) that genuinely are asynchronous.
        return Task.CompletedTask;
    }

    /// <summary>
    /// <see cref="LogSeverity"/>'s own remarks state its six levels are deliberately
    /// the same names and order as <c>Microsoft.Extensions.Logging.LogLevel</c> --
    /// Serilog's <see cref="LogEventLevel"/> uses a different five-level naming
    /// scheme (no separate Critical), so this mapping, not a direct enum cast, is the
    /// correct translation. <see cref="LogSeverity.Critical"/> maps to Serilog's own
    /// most severe level, <see cref="LogEventLevel.Fatal"/>.
    /// </summary>
    private static LogEventLevel ToSerilogLevel(LogSeverity severity) => severity switch
    {
        LogSeverity.Trace => LogEventLevel.Verbose,
        LogSeverity.Debug => LogEventLevel.Debug,
        LogSeverity.Information => LogEventLevel.Information,
        LogSeverity.Warning => LogEventLevel.Warning,
        LogSeverity.Error => LogEventLevel.Error,
        LogSeverity.Critical => LogEventLevel.Fatal,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unrecognized LogSeverity value."),
    };
}
