using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// A short, tenant-unique shift identifier. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md's identity table.
///
/// Uniqueness within the tenant is a cross-aggregate fact this type cannot see about
/// itself, so it arrives as a caller-supplied signal at <see cref="WorkShift"/>
/// construction, the established pattern for every prior module. What this type
/// enforces is shape: present, trimmed, and bounded.
/// </summary>
public sealed class ShiftCode : ValueObject
{
    public const int MaximumLength = 32;

    public string Value { get; }

    private ShiftCode(string value)
    {
        Value = value;
    }

    public static Result<ShiftCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ShiftCode>(TimekeepingErrors.ShiftCodeRequired);
        }

        var trimmed = value.Trim();
        return trimmed.Length > MaximumLength
            ? Result.Failure<ShiftCode>(TimekeepingErrors.ShiftCodeRequired)
            : Result.Success(new ShiftCode(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
