using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// The raw timing configuration for a <see cref="Schedule"/> -- a cron expression, an
/// ISO 8601 duration, or a specific timestamp, depending on <see cref="ScheduleType"/>.
/// Validated for shape only (required, reasonable length) -- the document gives no
/// single format to enforce across every <see cref="ScheduleType"/>, the identical
/// reasoning <c>SeriesKey</c>'s own remarks state for itself. Never parsed or evaluated
/// by this framework: computing an actual next-run timestamp from this value is Job
/// Processing Framework's own concern, per scheduling-framework.md's own Scope
/// exclusion ("Background Job Execution").
/// </summary>
public sealed class ScheduleExpression : ValueObject
{
    private const int _maxLength = 500;

    public string Value { get; }

    private ScheduleExpression(string value)
    {
        Value = value;
    }

    public static Result<ScheduleExpression> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ScheduleExpression>(SchedulingErrors.ScheduleExpressionRequired);
        }

        var trimmed = value.Trim();

        return trimmed.Length > _maxLength
            ? Result.Failure<ScheduleExpression>(SchedulingErrors.ScheduleExpressionTooLong)
            : Result.Success(new ScheduleExpression(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
