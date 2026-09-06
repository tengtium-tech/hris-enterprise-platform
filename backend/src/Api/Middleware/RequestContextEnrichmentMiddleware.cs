using Hris.Api.Http;
using Serilog.Context;

namespace Hris.Api.Middleware;

/// <summary>
/// Closes monitoring-and-alerting.md's own remaining Structured Logging gap
/// (NFR-OB-001): <c>CorrelationIdMiddleware</c> already carries <c>CorrelationId</c>
/// into every log entry this request produces; this middleware does the identical
/// job for <c>TenantId</c>/<c>UserId</c>, reading whatever
/// <see cref="RequestContext.SetTenantId"/>/<see cref="RequestContext.SetUserId"/>
/// already placed in <see cref="HttpContext.Items"/> earlier in the pipeline (see
/// <see cref="RequestContext"/>'s own remarks for why no concrete caller populates
/// them yet this Sprint) and pushing each onto Serilog's own <c>LogContext</c> for the
/// remainder of the request -- silently omitting whichever is absent, since a value
/// this middleware cannot yet obtain must never appear as a misleading placeholder
/// (an empty string or <see cref="Guid.Empty"/> logged as "the tenant id" would be
/// actively wrong, not merely incomplete).
///
/// Registered immediately after <c>UseCorrelationId()</c> in <c>Program.cs</c>, before
/// any endpoint or business logic executes, so every log entry the rest of the
/// pipeline produces -- including <c>UseSerilogRequestLogging()</c>'s own line -- is
/// already inside both scopes.
/// </summary>
internal sealed class RequestContextEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextEnrichmentMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantId = RequestContext.GetTenantId(context);
        var userId = RequestContext.GetUserId(context);

        using (tenantId is Guid tenantIdValue ? LogContext.PushProperty("TenantId", tenantIdValue) : EmptyScope.Instance)
        using (userId is Guid userIdValue ? LogContext.PushProperty("UserId", userIdValue) : EmptyScope.Instance)
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A no-op <see cref="IDisposable"/> standing in for <see cref="LogContext.PushProperty"/>
    /// when a value is absent -- a <c>using</c> statement needs some disposable on
    /// every path, and allocating a real, empty <c>LogContext</c> scope just to
    /// discard it immediately would be wasted work for the common case (this Sprint's
    /// own build: every request, since nothing populates <see cref="RequestContext"/>
    /// yet).
    /// </summary>
    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        private EmptyScope()
        {
        }

        public void Dispose()
        {
        }
    }
}

internal static class RequestContextEnrichmentMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextEnrichment(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestContextEnrichmentMiddleware>();
    }
}
