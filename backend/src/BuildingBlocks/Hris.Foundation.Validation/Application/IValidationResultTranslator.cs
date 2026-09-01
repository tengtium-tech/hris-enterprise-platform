using FluentValidation.Results;
using Hris.Foundation.Validation.Domain;

namespace Hris.Foundation.Validation.Application;

/// <summary>
/// The Application-owned port <see cref="Domain.ValidationSeverity"/>'s own remarks
/// already promise: "the platform's own vocabulary a future Infrastructure-layer
/// mapping translates FluentValidation's <c>ValidationResult</c> into." Defined here
/// in Application rather than Domain -- unlike <c>ILogSink</c>, which Logging
/// Framework's own Domain layer owns -- because a Domain-layer signature cannot
/// reference <see cref="FluentValidation.Results.ValidationResult"/> without pulling
/// the FluentValidation package into Domain, violating `CTR-ARC-001` ("Domain layer
/// has no outward dependencies"). FluentValidation is already an Application-layer-safe
/// dependency throughout this codebase -- every framework's own command validators
/// reference it directly from their own Application layer -- so putting the
/// boundary here, not in Domain, keeps that same rule intact while still giving
/// <see cref="ValidationService"/> an abstraction to depend on rather than the
/// concrete Infrastructure implementation (Clean Architecture's inward dependency
/// rule: Infrastructure may depend on Application, never the reverse).
/// </summary>
public interface IValidationResultTranslator
{
    ValidationOutcome Translate(ValidationResult result, ValidationPolicy policy);
}
