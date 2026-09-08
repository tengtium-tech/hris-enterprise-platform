using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The name of a <see cref="JobClassification"/> (examples: "Professional",
/// "Technical", "Administrative", "Executive"). Source:
/// docs/04-modules/position/domain/job-classifications.md, Business Characteristics.
/// Uniqueness (job-classifications.md: "Every Job Classification has a unique name")
/// is checked by the Application layer against the repository, not by this type or
/// by <see cref="JobClassification"/> itself.
/// </summary>
public sealed partial class JobClassificationName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private JobClassificationName(string value)
    {
        Value = value;
    }

    public static Result<JobClassificationName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobClassificationName>(PositionErrors.JobClassificationNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<JobClassificationName>(PositionErrors.JobClassificationNameTooLong)
            : Result.Success(new JobClassificationName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
