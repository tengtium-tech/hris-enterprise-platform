using System.Diagnostics;
using Hris.Foundation.Events.Domain;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Hris.Foundation.Events.Infrastructure.Publishing;

/// <summary>
/// Sprint 9 (Logging &amp; Monitoring, Operational Layer, HEP-89) -- closes
/// monitoring-and-alerting.md's own Distributed Tracing section (NFR-OB-002): "a
/// request that enqueues a... background job and returns immediately has, in a naive
/// implementation, ended its trace at the point of enqueueing... The trace context
/// (the CorrelationId from structured logging, and the OpenTelemetry trace and span
/// identifiers) is therefore propagated into the background job's own execution
/// context." <see cref="OutboxDispatcherBackgroundService"/> is the one real, already
/// running background execution engine this codebase has as of this Sprint (per this
/// framework's own established scope, Job Processing Framework's own worker process
/// is explicitly not built yet -- see that framework's own <c>DependencyInjection.cs</c>
/// remarks); <see cref="EventEnvelope"/> already carries the
/// <see cref="EventEnvelope.CorrelationId"/> and <see cref="EventEnvelope.TenantId"/>
/// fields needed to close this gap concretely, with no new field or migration
/// required.
///
/// Wraps one entry's own dispatch in both an OpenTelemetry <see cref="Activity"/>
/// (so a trace genuinely spans "request published the event -&gt; background
/// dispatch processed it," not two disconnected traces) and a Serilog
/// <see cref="LogContext"/> scope (so every log line the dispatch itself produces
/// carries the same <c>CorrelationId</c>/<c>TenantId</c> a request handling the
/// original business action already would) -- the identical "push request-scoped
/// context for the duration of the work" shape <c>CorrelationIdMiddleware</c>
/// established for an HTTP request, applied here to one background dispatch instead.
/// <see cref="EventEnvelope.TenantId"/> is optional (a <c>PlatformEvent</c> carries
/// none); the tag/property is simply omitted when absent, never a misleading
/// placeholder.
///
/// Extracted into its own static method, rather than inlined into
/// <see cref="OutboxDispatcherBackgroundService.DispatchOnceAsync"/>, so it is
/// directly unit-testable without a real database, DI scope, or hosted service --
/// only a real <see cref="OutboxEntry"/> and a delegate representing "the dispatch
/// work."
/// </summary>
internal static class OutboxDispatchEnrichment
{
    internal const string ActivitySourceName = "Hris.Foundation.Events.OutboxDispatcher";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    public static void DispatchWithEnrichment(OutboxEntry entry, Action dispatch)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(dispatch);

        using var activity = _activitySource.StartActivity("OutboxEntry.Dispatch");
        activity?.SetTag("correlation_id", entry.Envelope.CorrelationId.Value.ToString());
        if (entry.Envelope.TenantId is Guid tenantIdForTrace)
        {
            activity?.SetTag("tenant_id", tenantIdForTrace.ToString());
        }

        var enrichers = new List<ILogEventEnricher> { new PropertyEnricher("CorrelationId", entry.Envelope.CorrelationId.Value, false) };
        if (entry.Envelope.TenantId is Guid tenantIdForLog)
        {
            enrichers.Add(new PropertyEnricher("TenantId", tenantIdForLog, false));
        }

        using (LogContext.Push([.. enrichers]))
        {
            dispatch();
        }
    }
}
