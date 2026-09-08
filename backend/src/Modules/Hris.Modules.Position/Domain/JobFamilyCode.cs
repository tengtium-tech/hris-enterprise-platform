using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The unique short code identifying a <see cref="JobFamily"/> (examples: "IT",
/// "HR", "FIN", "OPS"). Source:
/// docs/04-modules/position/domain/value-objects.md, Job Family Code. Normalized to
/// upper invariant, the same convention <c>CostCenterCode</c> already establishes.
/// Uniqueness (job-families.md: "Every Job Family has a unique code") is checked by
/// the Application layer against the repository, not by this type or by
/// <see cref="JobFamily"/> itself.
/// </summary>
public sealed class JobFamilyCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private JobFamilyCode(string value)
    {
        Value = value;
    }

    public static Result<JobFamilyCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobFamilyCode>(PositionErrors.JobFamilyCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<JobFamilyCode>(PositionErrors.JobFamilyCodeTooLong)
            : Result.Success(new JobFamilyCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
