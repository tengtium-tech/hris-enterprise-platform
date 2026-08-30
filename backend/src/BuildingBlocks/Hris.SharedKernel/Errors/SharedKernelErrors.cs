namespace Hris.SharedKernel;

/// <summary>
/// The error catalog for value objects owned directly by SharedKernel itself (see
/// shared-kernel.md's own "What Belongs" list), following the same one-catalog-per-bounded-context
/// convention error-pattern.md establishes for every other framework and module.
/// </summary>
public static class SharedKernelErrors
{
    public static readonly Error EmailAddressRequired = new(
        "SharedKernel.EmailAddressRequired",
        "An email address is required.",
        ErrorCategory.Validation);

    public static readonly Error EmailAddressInvalidFormat = new(
        "SharedKernel.EmailAddressInvalidFormat",
        "The email address is not a valid format.",
        ErrorCategory.Validation);

    public static readonly Error CorrelationIdRequired = new(
        "SharedKernel.CorrelationIdRequired",
        "A correlation id is required.",
        ErrorCategory.Validation);

    public static readonly Error CurrencyCodeRequired = new(
        "SharedKernel.CurrencyCodeRequired",
        "A currency code is required.",
        ErrorCategory.Validation);

    public static readonly Error CurrencyCodeUnrecognized = new(
        "SharedKernel.CurrencyCodeUnrecognized",
        "The currency code is not a recognized ISO 4217 code.",
        ErrorCategory.Validation);

    public static readonly Error MoneyCurrencyMismatch = new(
        "SharedKernel.MoneyCurrencyMismatch",
        "Money values in different currencies cannot be combined directly.",
        ErrorCategory.Validation);
}
