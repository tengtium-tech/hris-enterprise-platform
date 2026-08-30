using Hris.Foundation.Logging.Domain;

namespace Hris.Foundation.Logging.Application;

/// <summary>
/// The Application-layer facade other code -- API middleware, background jobs, other
/// frameworks' and modules' own Application layers -- calls directly to emit a log
/// entry, per logging-framework.md's own framing: "Business modules and platform
/// services should use the Logging Framework for all operational logging instead of
/// implementing independent logging mechanisms."
///
/// Deliberately not a MediatR <c>ICommand</c>/<c>IQuery</c> the way Configuration
/// Framework's Application layer is built: nothing in logging-framework.md or in
/// this framework's own Domain layer (see <c>LogEntry</c>'s and <c>ILogSink</c>'s own
/// remarks) describes a user-driven business command lifecycle for a log entry --
/// it is generated once, synchronously, by whatever code needs to record an
/// operational fact, not submitted through an API endpoint the way a
/// <c>CreateConfigurationSettingCommand</c> is. Wrapping this in <c>ICommand</c>
/// would also route every log call through <c>TransactionBehavior</c>, which commits
/// a database transaction on success -- exactly the coupling this framework's own AI
/// Implementation Guidance prohibits ("Never depend on log output for business
/// behaviour").
/// </summary>
public interface ILoggingService
{
    /// <param name="severity">One of the six levels <c>LogSeverity</c> declares.</param>
    /// <param name="service">The originating service name (required).</param>
    /// <param name="correlationId">
    /// The correlation id already established for the current request, background job,
    /// or workflow action -- per logging-framework.md's Correlation ID section,
    /// propagated by the caller, never minted here. A fresh id minted per log call
    /// would defeat the one thing a correlation id is for: tying multiple log entries
    /// from the same request together.
    /// </param>
    /// <param name="message">The log message (required).</param>
    /// <param name="moduleName">The originating module, if applicable.</param>
    /// <param name="tenantId">
    /// The current tenant, if any -- left to the caller rather than resolved from
    /// ambient state here, since Logging Framework has no dependency on Tenant
    /// Framework (`CTR-ARC-002`, see <c>LogContext</c>'s own remarks).
    /// </param>
    /// <param name="userId">
    /// The current user, if any. Not auto-populated from Identity Framework: Identity's
    /// own Infrastructure layer (a real "current user" accessor) does not exist yet in
    /// this Sprint -- the same reasoning <c>HrisDbContext</c>'s own remarks give for not
    /// yet implementing automatic CreatedBy/ModifiedBy population. Add that resolution
    /// once Identity Framework's Infrastructure layer exists; do not invent a
    /// placeholder actor now.
    /// </param>
    /// <param name="exceptionDetails">Exception details, if this entry logs a failure.</param>
    /// <param name="metadata">
    /// Structured key/value metadata. Routed through <c>SensitiveDataScrubber</c> by
    /// <c>LogEntry.Create</c> before this method's caller ever sees the redacted form
    /// persisted -- callers do not need to scrub their own metadata first.
    /// </param>
    Task LogAsync(
        LogSeverity severity,
        string service,
        Guid correlationId,
        string message,
        string? moduleName = null,
        Guid? tenantId = null,
        Guid? userId = null,
        string? exceptionDetails = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
