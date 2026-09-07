using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The operating time zone of a <see cref="WorkLocation"/>. Source:
/// docs/04-modules/organization/domain/value-objects.md, TimeZone ("The value should
/// use IANA Time Zone identifiers."). Named <c>WorkLocationTimeZone</c> rather than
/// the document's own bare "TimeZone" to avoid colliding with .NET's own
/// <see cref="System.TimeZoneInfo"/> vocabulary in code that references both.
/// Validated against <see cref="TimeZoneInfo.TryFindSystemTimeZoneById"/> rather than
/// a hand-maintained IANA list, since .NET on every platform this codebase targets
/// (technology-stack.md: Linux containers) already resolves IANA identifiers
/// directly.
/// </summary>
public sealed class WorkLocationTimeZone : ValueObject
{
    public string Value { get; }

    private WorkLocationTimeZone(string value)
    {
        Value = value;
    }

    public static Result<WorkLocationTimeZone> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<WorkLocationTimeZone>(OrganizationErrors.TimeZoneRequired);
        }

        var normalized = value.Trim();

        return TimeZoneInfo.TryFindSystemTimeZoneById(normalized, out _)
            ? Result.Success(new WorkLocationTimeZone(normalized))
            : Result.Failure<WorkLocationTimeZone>(OrganizationErrors.TimeZoneInvalid);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
