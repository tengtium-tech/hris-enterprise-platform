using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// A free-text elaboration of the circumstance behind a <see cref="SeparationRecord"/>
/// -- for example, distinguishing Redundancy from Misconduct from Poor Performance
/// within a <see cref="SeparationType.Terminated"/> separation. Source:
/// docs/04-modules/employment/domain/value-objects.md's own TerminationReason
/// ("Examples" list is not presented as an exhaustive closed catalogue the way
/// <see cref="OperationalStatus"/>'s four values are, and several of its examples --
/// "Voluntary Resignation", "End of Contract" -- overlap conceptually with
/// <see cref="SeparationType"/> itself). Modeled as an optional free-text Value
/// Object carried alongside the required, closed-set <see cref="SeparationType"/> on
/// <see cref="SeparationRecord"/>, per entities.md's own Entity Usage table
/// ("SeparationRecord | SeparationType, TerminationReason, EffectiveDate").
/// </summary>
public sealed partial class TerminationReason : ValueObject
{
    private const int _maxLength = 500;

    public string Value { get; }

    private TerminationReason(string value)
    {
        Value = value;
    }

    public static Result<TerminationReason> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TerminationReason>(EmploymentErrors.TerminationReasonRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<TerminationReason>(EmploymentErrors.TerminationReasonTooLong)
            : Result.Success(new TerminationReason(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
