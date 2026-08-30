using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The unit of authorization, per authorization-framework.md's own Centralized
/// Evaluation section: "Permissions are the unit of authorization. Roles are
/// collections of permissions." One <see cref="PermissionKey"/> names an action on a
/// resource type -- e.g. "Employee:Read", "Payroll:Approve".
///
/// <see cref="ResourceType"/> is a validated string, not a closed enum, unlike
/// <see cref="PermissionAction"/>: this document's own Resource examples ("Employee
/// Record, Payroll, Attendance Record... Configuration, Report, API Endpoint") are
/// explicitly illustrative, and each of the nineteen business modules will register
/// its own resource types as it is built (Phase 2 onward) -- an enum here would
/// require a code change in this framework for every future module, which is exactly
/// the coupling a platform-wide authorization framework must not impose on modules
/// built years after it.
/// </summary>
public sealed class PermissionKey : ValueObject
{
    public string ResourceType { get; }

    public PermissionAction Action { get; }

    private PermissionKey(string resourceType, PermissionAction action)
    {
        ResourceType = resourceType;
        Action = action;
    }

    public static Result<PermissionKey> Create(string? resourceType, PermissionAction action)
    {
        return string.IsNullOrWhiteSpace(resourceType)
            ? Result.Failure<PermissionKey>(AuthorizationErrors.ResourceTypeRequired)
            : Result.Success(new PermissionKey(resourceType.Trim(), action));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ResourceType;
        yield return Action;
    }

    public override string ToString() => $"{ResourceType}:{Action}";
}
