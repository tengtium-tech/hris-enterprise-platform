using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Logging.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Logging.Application;

/// <summary>
/// The one implementation of <see cref="ILoggingService"/>. Builds the <see
/// cref="LogContext"/>/<see cref="LogEntry"/> the Domain layer owns, applies the
/// configured minimum severity threshold, and hands a passing entry to <see
/// cref="ILogSink"/> for collection -- the orchestration logging-framework.md's own
/// Log Lifecycle describes as "Generated -&gt; Collected," with everything from
/// "Aggregated" onward owned by whatever Infrastructure eventually implements <see
/// cref="ILogSink"/>.
///
/// Concretely exercises this framework's own "Upstream Dependencies: Configuration
/// Framework" line (logging-framework.md's Dependencies section) via <see
/// cref="ResolveConfigurationValueQuery"/> -- the same MediatR query every other
/// downstream consumer named in configuration-framework.md issues. The "Identity
/// Framework" and "Authorization Framework" upstream dependencies that same section
/// lists are deliberately not wired here: today, neither framework has an
/// Infrastructure layer a real "current user" accessor or a permission check could run
/// against (Identity remains Domain-only per backend/README.md), and Log Search --
/// the one capability an Authorization check would actually gate -- is explicitly not
/// built yet either (see <see cref="ILogSink"/>'s own remarks: "write-only by
/// design"). Both remain the caller-supplied optional fields <see cref="LogContext"/>
/// already declares, not silently invented here.
/// </summary>
internal sealed class LoggingService : ILoggingService
{
    /// <summary>
    /// The well-known Configuration Framework key this service resolves at Global
    /// scope for its minimum severity threshold. Not a Tenant-scoped lookup: Logging
    /// Framework has no dependency on Tenant Framework (`CTR-ARC-002`), the same reason
    /// <see cref="LogContext"/>'s own <c>TenantId</c> stays a raw, optional field
    /// instead of a strongly-typed Tenant reference.
    /// </summary>
    internal const string MinimumSeverityConfigurationKey = "Logging.MinimumSeverity";

    /// <summary>
    /// logging-framework.md's own principle -- "Production environments should
    /// minimize Trace and Debug logging" -- makes Information the safe default when no
    /// override is configured, rather than Trace (too permissive) or Warning (would
    /// silently drop Information-level operational logs no one asked to suppress).
    /// </summary>
    private const LogSeverity _defaultMinimumSeverity = LogSeverity.Information;

    private readonly ILogSink _sink;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public LoggingService(ILogSink sink, ISender sender, TimeProvider timeProvider)
    {
        _sink = Guard.AgainstNull(sink, nameof(sink));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task LogAsync(
        LogSeverity severity,
        string service,
        Guid correlationId,
        string message,
        string? moduleName = null,
        Guid? tenantId = null,
        Guid? userId = null,
        string? exceptionDetails = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var correlationIdResult = CorrelationId.Create(correlationId);
        if (correlationIdResult.IsFailure)
        {
            // An empty correlation id is a caller contract violation (every request,
            // background job, and event should already carry one per
            // logging-framework.md's own Correlation ID section), not a runtime
            // business outcome -- fails fast rather than being swallowed the way a
            // failed business command's Result would be reported back to a caller.
            throw new ArgumentException(correlationIdResult.Error.Description, nameof(correlationId));
        }

        var contextResult = LogContext.Create(correlationIdResult.Value, service, moduleName, tenantId, userId);
        if (contextResult.IsFailure)
        {
            throw new ArgumentException(contextResult.Error.Description, nameof(service));
        }

        var utcNow = _timeProvider.GetUtcNow();

        var entryResult = LogEntry.Create(severity, contextResult.Value, message, utcNow, exceptionDetails, metadata);
        if (entryResult.IsFailure)
        {
            throw new ArgumentException(entryResult.Error.Description, nameof(message));
        }

        var entry = entryResult.Value;

        var threshold = await ResolveMinimumSeverityAsync(utcNow, cancellationToken).ConfigureAwait(false);
        if (!entry.IsAtLeast(threshold))
        {
            return;
        }

        await _sink.WriteAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private async Task<LogSeverity> ResolveMinimumSeverityAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var query = new ResolveConfigurationValueQuery(
            MinimumSeverityConfigurationKey,
            [ConfigurationScope.Global()],
            DateOnly.FromDateTime(asOf.UtcDateTime));

        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        // No configured override resolves to the default rather than propagating
        // ConfigurationErrors.VersionNotFound -- logging-framework.md's own
        // Availability requirement ("Logging services should remain continuously
        // available") means the absence of an operator-set threshold must never stop
        // logging from working, the same reasoning AddHrisInfrastructure's connection
        // string check does NOT apply here (a missing database connection is fatal;
        // a missing logging-level override is not).
        return result.IsSuccess && Enum.TryParse<LogSeverity>(result.Value, ignoreCase: true, out var parsed)
            ? parsed
            : _defaultMinimumSeverity;
    }
}
