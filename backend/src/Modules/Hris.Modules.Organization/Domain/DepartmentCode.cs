using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The unique short code identifying a <see cref="Department"/> (examples: "HR",
/// "FIN", "IT"). Source: docs/04-modules/organization/domain/value-objects.md,
/// DepartmentCode. Normalized to upper invariant, matching every example in that
/// document. Uniqueness (DEPT-002, tenant-wide, a broader scope than DEPT-001's own
/// name uniqueness) is checked by the Application layer against the repository, not
/// by this type or by <see cref="Organization"/> itself.
/// </summary>
public sealed class DepartmentCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private DepartmentCode(string value)
    {
        Value = value;
    }

    public static Result<DepartmentCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<DepartmentCode>(OrganizationErrors.DepartmentCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<DepartmentCode>(OrganizationErrors.DepartmentCodeRequired)
            : Result.Success(new DepartmentCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
