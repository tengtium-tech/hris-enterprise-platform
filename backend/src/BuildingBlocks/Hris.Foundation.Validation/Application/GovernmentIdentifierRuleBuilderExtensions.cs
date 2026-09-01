using FluentValidation;
using Hris.Foundation.Validation.Domain;

namespace Hris.Foundation.Validation.Application;

/// <summary>
/// FluentValidation rule-builder extensions wrapping this framework's own four
/// Philippine government identifier Value Objects, per validation-framework.md's own
/// "Government Identifier Format" example under Validation Rule and its own
/// "Reusable Rules" principle: a business module's own command validator (once one
/// exists, Phase 2 onward) calls one of these instead of re-implementing the same
/// digit-count check inline, the same reuse <see cref="PagIbigNumber"/>,
/// <see cref="PhilHealthNumber"/>, <see cref="SssNumber"/>, and
/// <see cref="TaxIdentificationNumber"/> exist to enable but, until now, had no
/// caller anywhere in this codebase.
///
/// Each rule delegates entirely to its own Value Object's <c>Create</c> factory --
/// this class owns no format logic of its own, only the FluentValidation adapter
/// around it, so the two can never drift.
/// </summary>
public static class GovernmentIdentifierRuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string?> MustBeAValidTaxIdentificationNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => TaxIdentificationNumber.Create(value).IsSuccess)
            .WithErrorCode(ValidationErrors.TaxIdentificationNumberInvalidFormat.Code)
            .WithMessage(ValidationErrors.TaxIdentificationNumberInvalidFormat.Description);

    public static IRuleBuilderOptions<T, string?> MustBeAValidSssNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => SssNumber.Create(value).IsSuccess)
            .WithErrorCode(ValidationErrors.SssNumberInvalidFormat.Code)
            .WithMessage(ValidationErrors.SssNumberInvalidFormat.Description);

    public static IRuleBuilderOptions<T, string?> MustBeAValidPhilHealthNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => PhilHealthNumber.Create(value).IsSuccess)
            .WithErrorCode(ValidationErrors.PhilHealthNumberInvalidFormat.Code)
            .WithMessage(ValidationErrors.PhilHealthNumberInvalidFormat.Description);

    public static IRuleBuilderOptions<T, string?> MustBeAValidPagIbigNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => PagIbigNumber.Create(value).IsSuccess)
            .WithErrorCode(ValidationErrors.PagIbigNumberInvalidFormat.Code)
            .WithMessage(ValidationErrors.PagIbigNumberInvalidFormat.Description);
}
