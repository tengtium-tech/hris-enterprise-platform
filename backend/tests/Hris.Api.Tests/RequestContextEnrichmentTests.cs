using FluentAssertions;
using Hris.Api.Http;
using Hris.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// Sprint 9 (HEP-89): confirms <see cref="RequestContext"/>'s own round trip and
/// <see cref="RequestContextEnrichmentMiddleware"/>'s own log-context propagation
/// directly, against a real <see cref="DefaultHttpContext"/> and a real Serilog
/// logger, rather than the full <c>HrisApiFactory</c>/HTTP round trip
/// <see cref="CorrelationIdTests"/> uses -- no concrete production call site
/// populates <see cref="RequestContext"/> yet this Sprint (see that type's own
/// remarks), so there is no real endpoint that could exercise the "present" case
/// through an actual HTTP request; the middleware itself is fully real here, only
/// the population step a future authentication middleware will perform is
/// simulated.
/// </summary>
public sealed class RequestContextTests
{
    [Fact]
    public void GetTenantId_ReturnsNull_WhenNeverSet()
    {
        var context = new DefaultHttpContext();

        RequestContext.GetTenantId(context).Should().BeNull();
    }

    [Fact]
    public void SetTenantId_ThenGetTenantId_RoundTrips()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();

        RequestContext.SetTenantId(context, tenantId);

        RequestContext.GetTenantId(context).Should().Be(tenantId);
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenNeverSet()
    {
        var context = new DefaultHttpContext();

        RequestContext.GetUserId(context).Should().BeNull();
    }

    [Fact]
    public void SetUserId_ThenGetUserId_RoundTrips()
    {
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid();

        RequestContext.SetUserId(context, userId);

        RequestContext.GetUserId(context).Should().Be(userId);
    }
}

public sealed class RequestContextEnrichmentMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PushesTenantIdAndUserId_OntoLogContext_WhenBothArePresent()
    {
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        RequestContext.SetTenantId(context, tenantId);
        RequestContext.SetUserId(context, userId);

        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();
        var middleware = new RequestContextEnrichmentMiddleware(_ =>
        {
            logger.Information("inside pipeline");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        logEvent.Properties.Should().ContainKey("TenantId");
        logEvent.Properties["TenantId"].ToString().Should().Contain(tenantId.ToString());
        logEvent.Properties.Should().ContainKey("UserId");
        logEvent.Properties["UserId"].ToString().Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task InvokeAsync_OmitsBothProperties_WhenNeitherIsPresent()
    {
        var context = new DefaultHttpContext();
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();
        var middleware = new RequestContextEnrichmentMiddleware(_ =>
        {
            logger.Information("inside pipeline");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        logEvent.Properties.Should().NotContainKey("TenantId");
        logEvent.Properties.Should().NotContainKey("UserId");
    }

    [Fact]
    public async Task InvokeAsync_InvokesTheNextDelegate()
    {
        var context = new DefaultHttpContext();
        var invoked = false;
        var middleware = new RequestContextEnrichmentMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

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
