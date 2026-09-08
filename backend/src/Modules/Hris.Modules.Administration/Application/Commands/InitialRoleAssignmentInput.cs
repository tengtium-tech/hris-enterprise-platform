using Hris.Modules.Administration.Domain;

namespace Hris.Modules.Administration.Application.Commands;

/// <summary>
/// One initial role assignment carried by <see cref="ProvisionUserAccountCommand"/>.
/// Scoped to canonical roles only, matching <see cref="DelegatedAuthorityItem"/>'s
/// own documented scope decision -- granting a tenant-defined role at provisioning
/// time is supported through a follow-up <see cref="GrantRoleCommand"/> call
/// instead, keeping this input shape simple.
/// </summary>
public sealed record InitialRoleAssignmentInput(CanonicalRole Role, ScopeLevel ScopeLevel, Guid? ScopeTargetId, string? Reason);
