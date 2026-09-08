namespace Hris.Modules.Administration.Domain;

/// <summary>
/// One role-and-scope pair transferred by an <see cref="AdministrativeDelegation"/>.
/// Source: docs/04-modules/administration/domain/value-objects.md's
/// DelegatedAuthority ("The set of roles or administrative capabilities
/// transferred"). Scoped to canonical roles only this Sprint -- delegating a
/// tenant-defined <see cref="TenantRole"/> is a documented gap, since it would
/// require resolving the tenant role's own Published state at both delegation
/// creation and activation (delegated-administration.md's own "re-validated at
/// activation" requirement), doubling the cross-aggregate checks this Sprint's
/// simpler canonical-only model avoids. A plain record rather than a
/// <see cref="Domain.RoleReference"/> wrapper, since it carries no independent
/// identity of its own and needs only structural equality, which a record
/// provides without inheriting <c>ValueObject</c>.
/// </summary>
public sealed record DelegatedAuthorityItem(CanonicalRole Role, ScopeLevel ScopeLevel, Guid? ScopeTargetId);
