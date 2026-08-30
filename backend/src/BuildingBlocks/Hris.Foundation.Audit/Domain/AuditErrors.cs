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
}
