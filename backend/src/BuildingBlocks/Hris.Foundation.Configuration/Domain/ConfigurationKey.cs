using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The dot-segmented name identifying a configurable setting -- e.g.
/// "Payroll.GracePeriodMinutes" -- per configuration-framework.md's own Category
/// examples. A Value Object: immutable, self-validating, compared by value
/// (docs/02-architecture/04-domain-driven-design/value-objects.md).
/// </summary>
public sealed partial class ConfigurationKey : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private ConfigurationKey(string value)
    {
        Value = value;
    }

    public static Result<ConfigurationKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ConfigurationKey>(ConfigurationErrors.KeyRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<ConfigurationKey>(ConfigurationErrors.KeyTooLong);
        }

        if (!SegmentPattern().IsMatch(trimmed))
        {
            return Result.Failure<ConfigurationKey>(ConfigurationErrors.KeyInvalidFormat);
        }

        return Result.Success(new ConfigurationKey(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)*$")]
    private static partial Regex SegmentPattern();
}
