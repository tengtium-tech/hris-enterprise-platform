namespace Hris.Api.Http;

/// <summary>
/// Reads and writes the current request's own TenantId/UserId, stored in
/// <see cref="HttpContext.Items"/> the same way <c>CorrelationIdMiddleware</c> stores
/// its own correlation id -- a well-known slot any earlier-running middleware in the
/// pipeline populates, and <see cref="Middleware.RequestContextEnrichmentMiddleware"/>
/// reads back to enrich every log entry the rest of the pipeline produces, per
/// monitoring-and-alerting.md's own Structured Logging section (NFR-OB-001): "every
/// log entry carries, at minimum: CorrelationId, TenantId, UserId, Timestamp, Level."
///
/// No concrete caller populates these values yet this Sprint -- this backend has no
/// authentication middleware wired into the HTTP pipeline yet (Identity Framework,
/// Sprint 3, built the <c>AuthenticateCommand</c>/<c>UserAccount</c> Domain and
/// Application layers, not a Presentation-layer <c>UseAuthentication()</c> scheme),
/// and tenant-framework.md's own "Tenant Context Resolution" is itself a named,
/// not-yet-built Presentation-layer concern. Building either now, to give this
/// mechanism a concrete caller, would be exactly the scope creep CLAUDE.md's own
/// "work only the Sprint IMPLEMENTATION-PLAN.md says is current" warns against --
/// authentication and tenant resolution are their own, separate, not-yet-reached
/// pieces of work, not this Sprint's own stated deliverable. This is therefore a real
/// mechanism with no concrete caller yet, not a gap, the identical pattern this
/// codebase already applies to every framework's own unwired Upstream Dependency --
/// stated here explicitly rather than left implicit.
/// </summary>
internal static class RequestContext
{
    private const string _tenantIdItemKey = "TenantId";
    private const string _userIdItemKey = "UserId";

    public static void SetTenantId(HttpContext context, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[_tenantIdItemKey] = tenantId;
    }

    public static void SetUserId(HttpContext context, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[_userIdItemKey] = userId;
    }

    public static Guid? GetTenantId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(_tenantIdItemKey, out var value) && value is Guid tenantId ? tenantId : null;
    }

    public static Guid? GetUserId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(_userIdItemKey, out var value) && value is Guid userId ? userId : null;
    }
}
