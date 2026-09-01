using Hris.Foundation.Audit.Domain;

namespace Hris.Foundation.Audit.Application;

/// <summary>
/// The Application-layer facade audit-framework.md's own Overview calls for: "Every
/// business module should publish audit events through the Audit Framework rather
/// than implementing its own audit mechanism." Deliberately not a MediatR
/// <c>ICommand</c> the way Configuration/Identity/Authorization's own write
/// operations are -- the identical reasoning <c>ILoggingService</c>'s own remarks give
/// for its own facade shape, but sharper here: `CTR-AUD-003` requires a history/audit
/// record to be written *transactionally with the business change it describes*.
/// Routing this through its own <c>ICommand</c> would route it through
/// <c>TransactionBehavior</c>, forcing a second, separate <c>SaveChangesAsync</c> call
/// nested inside whatever business command is already recording the audit entry --
/// exactly the "multiple SaveChanges() calls in one business transaction" anti-pattern
/// dbcontext-design.md's own Common Anti-Patterns section prohibits. Calling this
/// facade directly, inline, from within another command's own handler -- before that
/// handler returns its own <c>Result.Success</c> -- lets the *caller's* own
/// <c>TransactionBehavior</c> cover the business change and the audit record together
/// in one <c>SaveChangesAsync</c> call, the identical "same transaction, for free"
/// mechanism <c>OutboxEventPublisher</c>'s own remarks document for Event Framework.
///
/// Returns a plain <see cref="Task"/>, not a <see cref="Hris.SharedKernel.Result"/>:
/// a caller invoking this already holds well-formed, already-validated data (it is
/// recording ITS OWN just-completed operation), so a validation failure here is a
/// caller contract violation, not a business outcome to report through -- the
/// identical reasoning <c>ILoggingService.LogAsync</c>'s own remarks give for the same
/// choice.
/// </summary>
public interface IAuditRecorder
{
    /// <param name="tenantId">
    /// Required even though <see cref="AuditRecord"/> itself carries no tenant field
    /// of its own -- this facade needs it only to populate the Event Framework
    /// envelope this call also publishes (see <see cref="AuditRecorder"/>'s own
    /// remarks), per that framework's own "Include tenant context in every event;
    /// consumers must not infer it" (`CTR-ISO-004`).
    /// </param>
    Task RecordAsync(
        AuditCategory category,
        string action,
        string businessEntity,
        string entityIdentifier,
        string sourceSystem,
        AuditResult outcome,
        Guid tenantId,
        Guid? actorId = null,
        string? previousValue = null,
        string? newValue = null,
        string? clientApplication = null,
        string? ipAddress = null,
        string? deviceInformation = null,
        Guid? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
