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

    /// <summary>
    /// Added alongside this framework's Application/Infrastructure layers, for
    /// <c>IOutboxEntryRepository.GetByIdAsync</c> returning <c>null</c> against an id a
    /// command was given -- e.g. <c>ReplayEventCommand</c>/<c>RequeueDeadLetterEventCommand</c>
    /// targeting an outbox row that no longer exists.
    /// </summary>
    public static readonly Error OutboxEntryNotFound = new(
        "Event.OutboxEntryNotFound",
        "The requested outbox entry does not exist.",
        ErrorCategory.NotFound);

    public static readonly Error OutboxEntryAlreadyDispatched = new(
        "Event.OutboxEntryAlreadyDispatched",
        "This outbox entry has already been dispatched and cannot record a failed attempt.",
        ErrorCategory.Conflict);

    /// <summary>
    /// <c>RequeueDeadLetterEventCommand</c>'s own precondition -- Dead Letter Queue's
    /// "Manual Recovery" is specifically for entries that exhausted retries, unlike
    /// <c>ReplayEventCommand</c>, which accepts any status. See
    /// <see cref="OutboxEntry.Requeue"/>'s own remarks for why the underlying Domain
    /// method itself stays permissive and this narrower precondition is enforced one
    /// layer up.
    /// </summary>
    public static readonly Error OutboxEntryNotDeadLettered = new(
        "Event.OutboxEntryNotDeadLettered",
        "Only a dead-lettered outbox entry can be recovered through manual DLQ recovery; use event replay instead.",
        ErrorCategory.Conflict);
}
