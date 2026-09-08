using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identifies a permission composed into a <see cref="TenantRole"/>. Source:
/// docs/04-modules/administration/domain/value-objects.md's PermissionReference
/// ("Must exist in the platform permission model"). Whether the named permission
/// actually exists in the platform's own permission model is a fact the
/// Authorization Framework owns, not this module -- validated here only for shape
/// (non-empty, a dotted `<module>.<action>`-style identifier), with existence
/// itself a documented gap; see <see cref="AdministrationErrors"/>'s own remarks.
/// </summary>
public sealed class PermissionReference : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private PermissionReference(string value)
    {
        Value = value;
    }

    public static Result<PermissionReference> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PermissionReference>(AdministrationErrors.PermissionReferenceRequired);
        }

        var normalized = value.Trim();
        return normalized.Length > _maxLength
            ? Result.Failure<PermissionReference>(AdministrationErrors.PermissionReferenceRequired)
            : Result.Success(new PermissionReference(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
