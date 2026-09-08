using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The nature of an Employment Contract -- Regular, Probationary, Fixed-Term, and so
/// on. Source: docs/04-modules/employment/domain/employment-contracts.md's own
/// "Contract Types" table ("Organizations may configure additional Contract Types
/// consistent with the Employment Type classifications"). Modeled as its own
/// tenant-configurable string Value Object, distinct from
/// <see cref="Domain.EmploymentType"/>, since it lives on a different Aggregate
/// (<see cref="EmploymentContract"/>) even though the two typically align.
/// </summary>
public sealed partial class ContractType : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private ContractType(string value)
    {
        Value = value;
    }

    public static Result<ContractType> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ContractType>(EmploymentErrors.ContractTypeRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<ContractType>(EmploymentErrors.ContractTypeTooLong)
            : Result.Success(new ContractType(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
