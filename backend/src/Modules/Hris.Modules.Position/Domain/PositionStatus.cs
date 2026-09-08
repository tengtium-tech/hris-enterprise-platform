namespace Hris.Modules.Position.Domain;

/// <summary>
/// Lifecycle status of a <see cref="Position"/>. Source:
/// docs/04-modules/position/domain/position-lifecycle.md's Lifecycle Model and State
/// Transitions sections, which name four stages (Draft, Active, Inactive, Archived)
/// and a concrete transition set: Draft-&gt;Active, Active-&gt;Inactive,
/// Inactive-&gt;Active, Active-&gt;Archived, Inactive-&gt;Archived. Unlike
/// Organization's own OrganizationalUnitStatus (resolved down to a two-state
/// Active/Archived model because that module's command/rule documents never
/// exercised a Draft or Inactive state), Position's own lifecycle document states a
/// concrete, non-narrative transition table that does include Draft and Inactive, so
/// all four states are modeled here as written.
/// </summary>
public enum PositionStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3,
}
