using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// A required, non-empty justification recorded with every role grant. Source:
/// docs/04-modules/administration/domain/value-objects.md's GrantReason ("Making
/// the field optional guarantees it will be empty in exactly the cases where it
/// matters. Construction fails without it").
/// </summary>
public sealed class GrantReason : ValueObject
{
    private const int _maxLength = 500;

    public string Value { get; }

    private GrantReason(string value)
    {
        Value = value;
    }

    public static Result<GrantReason> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<GrantReason>(AdministrationErrors.GrantReasonRequired);
        }

        var normalized = value.Trim();
        return normalized.Length > _maxLength
            ? Result.Failure<GrantReason>(AdministrationErrors.GrantReasonRequired)
            : Result.Success(new GrantReason(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
