using Hris.SharedKernel;

namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// One structured, immutable operational fact, per logging-framework.md's Log Entry
/// and Structured Logging sections. Deliberately not an <see cref="AggregateRoot{TId}"/>:
/// a log entry has no business lifecycle a caller drives through methods the way a
/// <c>ConfigurationSetting</c> does -- it is generated once, then only ever collected,
/// indexed, and eventually retired by *infrastructure* retention policy
/// (logging-framework.md's Log Lifecycle: "Generated -&gt; Collected -&gt; Aggregated
/// -&gt; Indexed -&gt; Stored -&gt; Archived -&gt; Deleted" describes a pipeline this
/// framework's Infrastructure layer will own, not Domain-layer business behavior).
/// For the same reason this type does not raise <see cref="IDomainEvent"/>s:
/// docs/02-architecture/04-domain-driven-design/domain-events.md's Event Ownership
/// section reserves that mechanism for Aggregates, and wiring business-style events
/// into logging risks the exact coupling this framework's own Implementation
/// Guidance prohibits ("Never depend on log output for business behaviour").
///
/// Excludes what this framework's own Scope section explicitly excludes -- "Business
/// Audit Trails, Business Workflow History" -- those belong to Audit Framework, built
/// later in this same Sprint 3.
/// </summary>
public sealed record LogEntry
{
    public Guid Id { get; }

    public DateTimeOffset TimestampUtc { get; }

    public LogSeverity Severity { get; }

    public LogContext Context { get; }

    public string Message { get; }

    public string? ExceptionDetails { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private LogEntry(
        Guid id,
        DateTimeOffset timestampUtc,
        LogSeverity severity,
        LogContext context,
        string message,
        string? exceptionDetails,
        IReadOnlyDictionary<string, string> metadata)
    {
        Id = id;
        TimestampUtc = timestampUtc;
        Severity = severity;
        Context = context;
        Message = message;
        ExceptionDetails = exceptionDetails;
        Metadata = metadata;
    }

    public static Result<LogEntry> Create(
        LogSeverity severity,
        LogContext context,
        string? message,
        DateTimeOffset timestampUtc,
        string? exceptionDetails = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Guard.AgainstNull(context, nameof(context));

        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure<LogEntry>(LoggingErrors.MessageRequired);
        }

        var redactedMetadata = SensitiveDataScrubber.Redact(metadata ?? new Dictionary<string, string>());

        return Result.Success(new LogEntry(
            Guid.NewGuid(),
            timestampUtc,
            severity,
            context,
            message.Trim(),
            exceptionDetails,
            redactedMetadata));
    }

    public bool IsAtLeast(LogSeverity threshold) => Severity >= threshold;
}
