using System.Diagnostics;
using FluentAssertions;
using Hris.Foundation.Events.Domain;
using Hris.Foundation.Events.Infrastructure.Publishing;
using Hris.SharedKernel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Hris.Foundation.Events.Tests.Infrastructure.Publishing;

/// <summary>
/// Sprint 9 (HEP-89): confirms <see cref="OutboxDispatchEnrichment.DispatchWithEnrichment"/>
/// genuinely closes monitoring-and-alerting.md's own "propagate trace context into
/// background work" requirement (NFR-OB-002) -- both halves, the OpenTelemetry
/// <see cref="Activity"/> and the Serilog <c>LogContext</c> scope, verified directly
/// against real Serilog/System.Diagnostics infrastructure, never a fake standing in
/// for either.
/// </summary>
public sealed class OutboxDispatchEnrichmentTests
{
    [Fact]
    public void DispatchWithEnrichment_InvokesTheGivenDispatchDelegate()
    {
        var entry = CreateEntry(tenantId: Guid.NewGuid());
        var invoked = false;

        OutboxDispatchEnrichment.DispatchWithEnrichment(entry, () => invoked = true);

        invoked.Should().BeTrue();
    }

    [Fact]
    public void DispatchWithEnrichment_PushesCorrelationIdAndTenantId_OntoLogContext_ForTheDurationOfDispatch()
    {
        var tenantId = Guid.NewGuid();
        var entry = CreateEntry(tenantId);
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();

        OutboxDispatchEnrichment.DispatchWithEnrichment(entry, () => logger.Information("dispatching"));

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        logEvent.Properties.Should().ContainKey("CorrelationId");
        logEvent.Properties["CorrelationId"].ToString().Should().Contain(entry.Envelope.CorrelationId.Value.ToString());
        logEvent.Properties.Should().ContainKey("TenantId");
        logEvent.Properties["TenantId"].ToString().Should().Contain(tenantId.ToString());
    }

    [Fact]
    public void DispatchWithEnrichment_OmitsTenantId_WhenTheEnvelopeCarriesNone()
    {
        var entry = CreateEntry(tenantId: null, category: EventCategory.PlatformEvent);
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();

        OutboxDispatchEnrichment.DispatchWithEnrichment(entry, () => logger.Information("dispatching"));

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        logEvent.Properties.Should().NotContainKey("TenantId");
    }

    [Fact]
    public void DispatchWithEnrichment_DoesNotLeakLogContextProperties_AfterDispatchCompletes()
    {
        var entry = CreateEntry(tenantId: Guid.NewGuid());
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();

        OutboxDispatchEnrichment.DispatchWithEnrichment(entry, () => { });
        logger.Information("after dispatch completed");

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        logEvent.Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void DispatchWithEnrichment_StartsAnActivity_TaggedWithCorrelationIdAndTenantId()
    {
        var tenantId = Guid.NewGuid();
        var entry = CreateEntry(tenantId);
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OutboxDispatchEnrichment.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = stoppedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        OutboxDispatchEnrichment.DispatchWithEnrichment(entry, () => { });

        var activity = stoppedActivities.Should().ContainSingle().Subject;
        activity.Tags.Should().Contain(tag => tag.Key == "correlation_id" && tag.Value == entry.Envelope.CorrelationId.Value.ToString());
        activity.Tags.Should().Contain(tag => tag.Key == "tenant_id" && tag.Value == tenantId.ToString());
    }

    private static OutboxEntry CreateEntry(Guid? tenantId, EventCategory category = EventCategory.DomainEvent)
    {
        var envelope = EventEnvelope.Create(
            new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
            sourceModule: "Hris.Foundation.Events.Tests",
            category,
            CorrelationId.NewId(),
            payload: "{}",
            tenantId).Value;

        return OutboxEntry.Create(envelope, DateTimeOffset.UtcNow).Value;
    }

    private sealed record TestDomainEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

    private sealed class DelegatingSink : ILogEventSink
    {
        private readonly Action<LogEvent> _onEmit;

        public DelegatingSink(Action<LogEvent> onEmit)
        {
            _onEmit = onEmit;
        }

        public void Emit(LogEvent logEvent) => _onEmit(logEvent);
    }
}
