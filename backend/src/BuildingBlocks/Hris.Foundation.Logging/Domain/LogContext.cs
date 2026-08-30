using Hris.SharedKernel;

namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// The "who/where" every <see cref="LogEntry"/> carries, per logging-framework.md's
/// Log Entry section (Service, Module, Correlation ID, Tenant ID, User ID) and its
/// own Implementation Guidance: "Emit structured logs including correlation,
/// tenant, and user identifiers (`NFR-OB-001`)."
///
/// <see cref="TenantId"/> and <see cref="UserId"/> are raw, optional
/// <see cref="Guid"/> values rather than strongly typed references into Tenant
/// Framework (Sprint 4) or Identity Framework (built later in this same Sprint 3):
/// Logging Framework has no dependency on either (`CTR-ARC-002`), and a log entry
/// must remain emittable for anonymous/unauthenticated and background-job activity
/// where no user, and in a handful of platform-bootstrap paths no tenant, exists yet.
/// </summary>
public sealed class LogContext : ValueObject
{
    public CorrelationId CorrelationId { get; }

    public string Service { get; }

    public string? Module { get; }

    public Guid? TenantId { get; }

    public Guid? UserId { get; }

    private LogContext(CorrelationId correlationId, string service, string? module, Guid? tenantId, Guid? userId)
    {
        CorrelationId = correlationId;
        Service = service;
        Module = module;
        TenantId = tenantId;
        UserId = userId;
    }

    public static Result<LogContext> Create(
        CorrelationId correlationId,
        string? service,
        string? module = null,
        Guid? tenantId = null,
        Guid? userId = null)
    {
        Guard.AgainstNull(correlationId, nameof(correlationId));

        if (string.IsNullOrWhiteSpace(service))
        {
            return Result.Failure<LogContext>(LoggingErrors.ServiceRequired);
        }

        return Result.Success(new LogContext(correlationId, service.Trim(), module?.Trim(), tenantId, userId));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CorrelationId;
        yield return Service;
        yield return Module;
        yield return TenantId;
        yield return UserId;
    }
}
