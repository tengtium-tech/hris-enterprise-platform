using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class ValidationErrors
{
    public static readonly Error TaxIdentificationNumberInvalidFormat = new(
        "Validation.TaxIdentificationNumberInvalidFormat",
        "A Philippine Tax Identification Number must be 9 digits, or 12 with a branch code.",
        ErrorCategory.Validation);

    public static readonly Error SssNumberInvalidFormat = new(
        "Validation.SssNumberInvalidFormat",
        "A Philippine SSS number must be 10 digits.",
        ErrorCategory.Validation);

    public static readonly Error PhilHealthNumberInvalidFormat = new(
        "Validation.PhilHealthNumberInvalidFormat",
        "A Philippine PhilHealth number must be 12 digits.",
        ErrorCategory.Validation);

    public static readonly Error PagIbigNumberInvalidFormat = new(
        "Validation.PagIbigNumberInvalidFormat",
        "A Philippine Pag-IBIG (HDMF) number must be 12 digits.",
        ErrorCategory.Validation);
}
