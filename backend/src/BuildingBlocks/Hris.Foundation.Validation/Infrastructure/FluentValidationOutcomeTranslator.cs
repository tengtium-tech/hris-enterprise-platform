using FluentValidation.Results;
using Hris.Foundation.Validation.Application;
using Hris.Foundation.Validation.Domain;
using Hris.SharedKernel;
using DomainValidationFailure = Hris.Foundation.Validation.Domain.ValidationFailure;

namespace Hris.Foundation.Validation.Infrastructure;

/// <summary>
/// The Infrastructure-layer adapter <see cref="ValidationSeverity"/>'s own remarks
/// call for -- "a future Infrastructure-layer mapping translates FluentValidation's
/// ValidationResult into" this framework's own vocabulary. The one place
/// <c>FluentValidation.Results.ValidationFailure</c> is read anywhere in this
/// framework; everywhere else (<see cref="Application.GovernmentIdentifierRuleBuilderExtensions"/>,
/// any future business module's own command validator) only ever writes rules
/// against FluentValidation's own fluent API, never reads its result shape back.
/// </summary>
internal sealed class FluentValidationOutcomeTranslator : IValidationResultTranslator
{
    /// <summary>
    /// FluentValidation's own <c>ErrorCode</c> is empty whenever a rule does not call
    /// <c>.WithErrorCode(...)</c> explicitly -- most of the built-in rules
    /// (<c>NotEmpty</c>, <c>MaximumLength</c>, and so on) do not. <see cref="DomainValidationFailure"/>'s
    /// own constructor guards against a null-or-whitespace error code (this
    /// framework's own error-pattern.md discipline: every failure is
    /// self-describing), so an unset FluentValidation error code maps to this
    /// placeholder rather than throwing at translation time for a condition the
    /// caller's own validator, not this translator, is responsible for.
    /// </summary>
    internal const string UnspecifiedErrorCode = "Validation.Unspecified";

    public ValidationOutcome Translate(ValidationResult result, ValidationPolicy policy)
    {
        Guard.AgainstNull(result, nameof(result));

        var failures = result.Errors
            .Select(failure => new DomainValidationFailure(
                failure.PropertyName,
                string.IsNullOrWhiteSpace(failure.ErrorCode) ? UnspecifiedErrorCode : failure.ErrorCode,
                failure.ErrorMessage,
                ToValidationSeverity(failure.Severity)))
            .ToList();

        return new ValidationOutcome(failures, policy);
    }

    /// <summary>
    /// <see cref="ValidationSeverity"/>'s own remarks state this framework's four
    /// levels are "deliberately richer than FluentValidation's own built-in Severity
    /// enum (Error, Warning, Info only, no Critical)" -- this mapping is that
    /// document's own promised translation. Nothing here ever produces
    /// <see cref="ValidationSeverity.Critical"/>: FluentValidation itself has no
    /// concept of it, so a Critical <see cref="DomainValidationFailure"/> can only
    /// ever originate from code that constructs one directly, bypassing FluentValidation
    /// entirely -- not from this translator.
    /// </summary>
    private static ValidationSeverity ToValidationSeverity(FluentValidation.Severity severity) => severity switch
    {
        FluentValidation.Severity.Error => ValidationSeverity.Error,
        FluentValidation.Severity.Warning => ValidationSeverity.Warning,
        FluentValidation.Severity.Info => ValidationSeverity.Information,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unrecognized FluentValidation Severity value."),
    };
}
