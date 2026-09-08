using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The name of a <see cref="JobGrade"/> (examples: "Grade 1", "Senior Professional",
/// "Director"). Source: docs/04-modules/position/domain/job-grades.md, Business
/// Characteristics. Uniqueness (job-grades.md: "Every Job Grade has a unique name")
/// is checked by the Application layer against the repository, not by this type or
/// by <see cref="JobGrade"/> itself.
/// </summary>
public sealed partial class JobGradeName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private JobGradeName(string value)
    {
        Value = value;
    }

    public static Result<JobGradeName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobGradeName>(PositionErrors.JobGradeNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<JobGradeName>(PositionErrors.JobGradeNameTooLong)
            : Result.Success(new JobGradeName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
