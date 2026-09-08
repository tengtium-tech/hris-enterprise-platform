using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The Employment Level dimension of Employment Category -- Rank-and-File,
/// Supervisory, Managerial, Executive. Source:
/// docs/04-modules/employment/domain/employment-categories.md's own "Employment
/// Level" dimension table ("Categories are configured per tenant"). The other two
/// documented dimensions are modeled separately: Work Arrangement as
/// <see cref="Domain.WorkArrangement"/> (owned by <see cref="EmploymentAssignment"/>,
/// per value-objects.md), and Compensation Basis as
/// <see cref="Domain.CompensationBasis"/> (carried on <see cref="CompensationAmount"/>).
/// Modeled as a tenant-configurable string, matching Position Type's and Employment
/// Type's own precedent, since the dimension is explicitly tenant-configured rather
/// than a fixed platform catalog.
/// </summary>
public sealed partial class EmploymentCategory : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private EmploymentCategory(string value)
    {
        Value = value;
    }

    public static Result<EmploymentCategory> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmploymentCategory>(EmploymentErrors.EmploymentCategoryRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<EmploymentCategory>(EmploymentErrors.EmploymentCategoryTooLong)
            : Result.Success(new EmploymentCategory(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
