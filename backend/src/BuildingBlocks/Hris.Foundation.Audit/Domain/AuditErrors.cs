using Hris.SharedKernel;

namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class AuditErrors
{
    public static readonly Error ActionRequired = new(
        "Audit.ActionRequired",
        "An audit record's action is required.",
        ErrorCategory.Validation);

    public static readonly Error BusinessEntityRequired = new(
        "Audit.BusinessEntityRequired",
        "An audit record's business entity type is required.",
        ErrorCategory.Validation);

    public static readonly Error EntityIdentifierRequired = new(
        "Audit.EntityIdentifierRequired",
        "An audit record's entity identifier is required.",
        ErrorCategory.Validation);

    public static readonly Error SourceSystemRequired = new(
        "Audit.SourceSystemRequired",
        "An audit record's source system is required.",
        ErrorCategory.Validation);

    /// <summary>
    /// Added alongside this framework's Application layer, for
    /// <c>SearchAuditRecordsQuery</c>/<c>GetAuditRecordByIdQuery</c> -- audit-framework.md's
    /// own Security Considerations: "Only authorized users should access audit
    /// information." A category of <see cref="ErrorCategory.Authorization"/>, unlike
    /// Identity Framework's own <c>AuthenticationFailed</c>: there is no account-
    /// enumeration risk in an authorization denial the way there is in a failed login
    /// attempt, so the specific, distinguishable reason is safe to return.
    /// </summary>
    public static readonly Error NotAuthorizedToAccessAuditRecords = new(
        "Audit.NotAuthorizedToAccessAuditRecords",
        "The caller is not authorized to access audit records.",
        ErrorCategory.Authorization);

    public static readonly Error AuditRecordNotFound = new(
        "Audit.AuditRecordNotFound",
        "The requested audit record does not exist.",
        ErrorCategory.NotFound);
}
