using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The name of a <see cref="JobFamily"/> (examples: "Information Technology",
/// "Human Resources"). Source: docs/04-modules/position/domain/job-families.md,
/// Business Characteristics. Uniqueness (job-families.md: "Every Job Family has a
/// unique name") is checked by the Application layer against the repository, not by
/// this type or by <see cref="JobFamily"/> itself.
/// </summary>
public sealed partial class JobFamilyName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private JobFamilyName(string value)
    {
        Value = value;
    }

    public static Result<JobFamilyName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobFamilyName>(PositionErrors.JobFamilyNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<JobFamilyName>(PositionErrors.JobFamilyNameTooLong)
            : Result.Success(new JobFamilyName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
