using Serilog.Context;

namespace Hris.Api.Middleware;

/// <summary>
/// api-standards.md's own Error Response Format section: "correlationId ties the
/// error response to the structured log entry monitoring-and-alerting.md already
/// requires every request to carry, so a support engineer can go from a reported
/// error directly to the matching log entry." This middleware is what makes that
/// true end to end: the same id is echoed on the response header, carried into
/// every Serilog log line this request produces (including
/// <c>UseSerilogRequestLogging()</c>'s own line, already wired in <c>Program.cs</c>),
/// and read back by <see cref="GetCorrelationId"/> when a failed <c>Result</c> is
/// turned into a Problem Details body.
///
/// A caller-supplied <see cref="HeaderName"/> is honored, not overwritten -- a client
/// tracing a request across its own multiple downstream calls needs the same
/// correlation id to survive the hop into this platform, not a new one minted here
/// that breaks that trace.
/// </summary>
internal sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string _httpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[_httpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(_httpContextItemKey, correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    public static string GetCorrelationId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(_httpContextItemKey, out var value) && value is string correlationId
            ? correlationId
            : string.Empty;
    }
}

internal static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
