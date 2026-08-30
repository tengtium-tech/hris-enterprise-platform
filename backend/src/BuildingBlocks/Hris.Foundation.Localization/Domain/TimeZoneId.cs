using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// An IANA time zone identifier (e.g. "Asia/Manila"), per localization-framework.md's
/// Time Zone section: "Tenant Time Zone, Company Time Zone, User Time Zone, UTC
/// Storage, Daylight Saving Time (DST)... All timestamps should be stored in UTC and
/// presented according to user preferences." Validated through
/// <see cref="TimeZoneInfo.FindSystemTimeZoneById"/> -- the runtime's own IANA/Windows
/// time zone database, not a hand-maintained list, and DST rules stay entirely the
/// BCL's problem rather than this platform's.
/// </summary>
public sealed class TimeZoneId : ValueObject
{
    public string Value { get; }

    private TimeZoneId(string value)
    {
        Value = value;
    }

    public static Result<TimeZoneId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TimeZoneId>(LocalizationErrors.TimeZoneIdRequired);
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.Trim());
            return Result.Success(new TimeZoneId(timeZone.Id));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return Result.Failure<TimeZoneId>(LocalizationErrors.TimeZoneIdUnrecognized);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
