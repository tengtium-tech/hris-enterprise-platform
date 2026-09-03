using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// The IANA time zone identifier (for example "Asia/Manila") a <see cref="Schedule"/>
/// is evaluated in -- scheduling-framework.md's own AI Implementation Guidance: "Evaluate
/// schedules in the tenant's time zone, not the server's." Validated for shape only
/// (required, reasonable length), not against the OS/ICU time zone database: that
/// database's own availability and exact identifier set is a runtime/environment
/// concern, and this Domain layer's own factory must stay a pure function per
/// docs/09-testing/unit-and-integration-testing.md 2.1 ("must not touch... the file
/// system, environment, or any I/O source").
/// </summary>
public sealed class ScheduleTimeZone : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private ScheduleTimeZone(string value)
    {
        Value = value;
    }

    public static Result<ScheduleTimeZone> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ScheduleTimeZone>(SchedulingErrors.TimeZoneRequired);
        }

        var trimmed = value.Trim();

        return trimmed.Length > _maxLength
            ? Result.Failure<ScheduleTimeZone>(SchedulingErrors.TimeZoneTooLong)
            : Result.Success(new ScheduleTimeZone(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
