using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The unique short code identifying a <see cref="JobClassification"/> (examples:
/// "PROF", "TECH", "ADMIN", "EXEC"). Source:
/// docs/04-modules/position/domain/value-objects.md, Job Classification Code.
/// Normalized to upper invariant. Uniqueness (business-rules.md: "Classification
/// codes are unique") is checked by the Application layer against the repository,
/// not by this type or by <see cref="JobClassification"/> itself.
/// </summary>
public sealed class JobClassificationCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private JobClassificationCode(string value)
    {
        Value = value;
    }

    public static Result<JobClassificationCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobClassificationCode>(PositionErrors.JobClassificationCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<JobClassificationCode>(PositionErrors.JobClassificationCodeTooLong)
            : Result.Success(new JobClassificationCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
