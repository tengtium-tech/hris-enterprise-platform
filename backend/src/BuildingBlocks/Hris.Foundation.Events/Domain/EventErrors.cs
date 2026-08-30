using Hris.SharedKernel;

namespace Hris.Foundation.Events.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class EventErrors
{
    public static readonly Error SourceModuleRequired = new(
        "Event.SourceModuleRequired",
        "An event envelope's source module is required.",
        ErrorCategory.Validation);

    public static readonly Error TenantIdRequiredForScopedEvent = new(
        "Event.TenantIdRequiredForScopedEvent",
        "A Domain Event or Integration Event must carry a tenant id (`CTR-ISO-004`); only a Platform Event may omit one.",
        ErrorCategory.Validation);
}
