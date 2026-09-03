using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class NumberingErrors
{
    public static readonly Error SeriesKeyRequired = new(
        "Numbering.SeriesKeyRequired",
        "A series key is required.",
        ErrorCategory.Validation);

    public static readonly Error SeriesKeyTooLong = new(
        "Numbering.SeriesKeyTooLong",
        "A series key cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error SeriesKeyAlreadyRegistered = new(
        "Numbering.SeriesKeyAlreadyRegistered",
        "A number series is already registered under this key.",
        ErrorCategory.Conflict);

    public static readonly Error NumberSeriesNotFound = new(
        "Numbering.NumberSeriesNotFound",
        "No number series exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error PrefixRequired = new(
        "Numbering.PrefixRequired",
        "A prefix is required.",
        ErrorCategory.Validation);

    public static readonly Error PrefixInvalid = new(
        "Numbering.PrefixInvalid",
        "A prefix must be 1-10 uppercase letters or digits.",
        ErrorCategory.Validation);

    public static readonly Error RunningNumberLengthOutOfRange = new(
        "Numbering.RunningNumberLengthOutOfRange",
        "The running-number length must be between 1 and 10 digits.",
        ErrorCategory.Validation);

    public static readonly Error SeparatorRequired = new(
        "Numbering.SeparatorRequired",
        "A separator is required.",
        ErrorCategory.Validation);

    public static readonly Error SeparatorTooLong = new(
        "Numbering.SeparatorTooLong",
        "A separator cannot exceed 3 characters.",
        ErrorCategory.Validation);

    public static readonly Error FormattedNumberRequired = new(
        "Numbering.FormattedNumberRequired",
        "A formatted number is required.",
        ErrorCategory.Validation);

    public static readonly Error FormattedNumberTooLong = new(
        "Numbering.FormattedNumberTooLong",
        "A formatted number cannot exceed 100 characters.",
        ErrorCategory.Validation);

    public static readonly Error IssuedNumberNotFound = new(
        "Numbering.IssuedNumberNotFound",
        "No issued number exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidNumberLifecycleTransition = new(
        "Numbering.InvalidNumberLifecycleTransition",
        "This transition is not valid from the number's current status.",
        ErrorCategory.Domain);

    public static readonly Error AssignedToTypeRequired = new(
        "Numbering.AssignedToTypeRequired",
        "An assigned-to type is required.",
        ErrorCategory.Validation);

    public static readonly Error AssignedToReferenceIdRequired = new(
        "Numbering.AssignedToReferenceIdRequired",
        "An assigned-to reference id is required.",
        ErrorCategory.Validation);

    public static readonly Error ReleaseReasonRequired = new(
        "Numbering.ReleaseReasonRequired",
        "A reason is required to release an issued number.",
        ErrorCategory.Validation);

    public static readonly Error NumberFormatMismatch = new(
        "Numbering.NumberFormatMismatch",
        "The formatted number no longer matches its series' own current prefix and format.",
        ErrorCategory.Domain);
}
