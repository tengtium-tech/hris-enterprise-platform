using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// A queue's own stable, unique key -- job-processing.md's own Core Concepts examples
/// ("Payroll Queue", "Notification Queue", "Import Queue"). Validated for shape only
/// (required, reasonable length) -- the document gives no explicit key format to
/// enforce, the identical reasoning <c>SeriesKey</c>'s own remarks state for itself.
/// </summary>
public sealed class JobQueueName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private JobQueueName(string value)
    {
        Value = value;
    }

    public static Result<JobQueueName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<JobQueueName>(JobProcessingErrors.JobQueueNameRequired);
        }

        var trimmed = value.Trim();

        return trimmed.Length > _maxLength
            ? Result.Failure<JobQueueName>(JobProcessingErrors.JobQueueNameTooLong)
            : Result.Success(new JobQueueName(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
