using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// One problem found during validation, per validation-framework.md's Validation
/// Result section ("Error Code, Error Message, Warning Message, Field Name,
/// Validation Rule, Severity"). Constructed directly through Guard clauses, not a
/// Result-returning factory: an ill-formed <see cref="ValidationFailure"/> (a missing
/// error code, say) is a programmer mistake in whatever code is reporting the
/// problem, not an expected business outcome this type itself needs to negotiate --
/// guard-clauses.md's own distinction between the two.
/// </summary>
public sealed class ValidationFailure : ValueObject
{
    public string FieldName { get; }

    public string ErrorCode { get; }

    public string Message { get; }

    public ValidationSeverity Severity { get; }

    public ValidationFailure(string fieldName, string errorCode, string message, ValidationSeverity severity)
    {
        FieldName = Guard.AgainstNullOrWhiteSpace(fieldName, nameof(fieldName));
        ErrorCode = Guard.AgainstNullOrWhiteSpace(errorCode, nameof(errorCode));
        Message = Guard.AgainstNullOrWhiteSpace(message, nameof(message));
        Severity = severity;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldName;
        yield return ErrorCode;
        yield return Severity;
    }

    public override string ToString() => $"[{Severity}] {FieldName}: {Message} ({ErrorCode})";
}
