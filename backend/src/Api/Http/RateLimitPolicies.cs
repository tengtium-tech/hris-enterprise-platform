using System.Globalization;
using System.Threading.RateLimiting;
using Hris.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace Hris.Api.Http;

/// <summary>
/// api-standards.md's own Rate Limiting section names the endpoint classes this
/// applies to -- "search, lookup-by-identifier, export, and any authority-granting
/// or decision-making action" -- and states the reasoning plainly: "a security
/// control, not primarily a capacity one." Each class gets its own named policy so a
/// future endpoint opts in by class (`.RequireRateLimiting(RateLimitPolicies.Export)`,
/// say) rather than every endpoint sharing one global limit that fits none of them
/// well.
///
/// Only <see cref="LookupByIdentifier"/> is wired to an actual endpoint this Sprint
/// (<c>OperationsEndpoints</c>) -- the other three class names are reserved here so
/// a future module's own endpoint picks the correct policy by name rather than each
/// module inventing its own limiter configuration independently.
/// </summary>
internal static class RateLimitPolicies
{
    public const string LookupByIdentifier = "lookup-by-identifier";
    public const string Search = "search";
    public const string Export = "export";
    public const string AuthorityGranting = "authority-granting";
}

internal static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// api-standards.md's own Rate Limiting header table:
    /// <c>RateLimit-Limit</c>/<c>RateLimit-Remaining</c>/<c>RateLimit-Reset</c>/
    /// <c>Retry-After</c>. Set on the <c>429</c> response only, per that same
    /// section's own explicit "Present only on a 429 response" note for
    /// <c>Retry-After</c> -- emitting the other three on every successful response
    /// too would need this platform's own limiter instrumented per-request beyond
    /// what <c>Microsoft.AspNetCore.RateLimiting</c>'s own rejection callback
    /// exposes; a stated gap for a future pass, not silently assumed solved here.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        const int permitLimit = 100;
        var window = TimeSpan.FromMinutes(1);

        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, cancellationToken) =>
            {
                var windowSeconds = ((int)window.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                context.HttpContext.Response.Headers["RateLimit-Limit"] = permitLimit.ToString(CultureInfo.InvariantCulture);
                context.HttpContext.Response.Headers["RateLimit-Remaining"] = "0";
                context.HttpContext.Response.Headers["RateLimit-Reset"] = windowSeconds;
                context.HttpContext.Response.Headers["Retry-After"] = windowSeconds;

                var problem = Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Rate limit exceeded.",
                    type: "https://docs.hris.example/errors/rate_limit.exceeded",
                    instance: context.HttpContext.Request.Path,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "rate_limit.exceeded",
                        ["correlationId"] = CorrelationIdMiddleware.GetCorrelationId(context.HttpContext),
                    });

                await problem.ExecuteAsync(context.HttpContext).ConfigureAwait(false);
            };

            foreach (var policyName in new[]
                     {
                         RateLimitPolicies.LookupByIdentifier,
                         RateLimitPolicies.Search,
                         RateLimitPolicies.Export,
                         RateLimitPolicies.AuthorityGranting,
                     })
            {
                options.AddFixedWindowLimiter(policyName, limiterOptions =>
                {
                    limiterOptions.PermitLimit = permitLimit;
                    limiterOptions.Window = window;
                    limiterOptions.QueueLimit = 0;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            }
        });

        return services;
    }
}
