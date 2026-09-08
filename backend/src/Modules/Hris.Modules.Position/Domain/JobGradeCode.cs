using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The unique short code identifying a <see cref="JobGrade"/> (examples: "G01",
/// "G08", "M02", "EX01"). Source:
/// docs/04-modules/position/domain/value-objects.md, Job Grade Code. Normalized to
/// upper invariant. Uniqueness (business-rules.md: "Grade Codes are unique") is
/// checked by the Application layer against the repository, not by this type or by
/// <see cref="JobGrade"/> itself.
/// </summary>
public sealed class JobGradeCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private JobGradeCode(string value)
    {
        Value = value;
    }

    public static Result<JobGradeCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobGradeCode>(PositionErrors.JobGradeCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<JobGradeCode>(PositionErrors.JobGradeCodeTooLong)
            : Result.Success(new JobGradeCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
