namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// The four levels validation-framework.md's Validation Severity section names:
/// "Information, Warning, Error, Critical... determines processing behavior."
/// Deliberately richer than FluentValidation's own built-in <c>Severity</c> enum
/// (Error, Warning, Info only, no Critical) -- this is the platform's own vocabulary
/// a future Infrastructure-layer mapping translates FluentValidation's
/// <c>ValidationResult</c> into, adding the fourth level this document requires and
/// FluentValidation does not have.
/// </summary>
public enum ValidationSeverity
{
    Information = 0,
    Warning,
    Error,
    Critical,
}
