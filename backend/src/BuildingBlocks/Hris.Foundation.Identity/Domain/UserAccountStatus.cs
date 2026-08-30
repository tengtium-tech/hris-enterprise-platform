namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// The six states identity-framework.md's Identity Lifecycle section names: "Invited,
/// Active, Locked, Suspended, Disabled, Archived." Unlike
/// <c>ConfigurationLifecycleState</c>, this is not a strict total order -- Locked and
/// Suspended both return to Active, so <see cref="UserAccount"/>'s own transition
/// methods each validate against an explicit allowed-predecessor set rather than "the
/// immediately next enum value." See <see cref="UserAccount"/>'s transition methods
/// for the exact allowed graph.
/// </summary>
public enum UserAccountStatus
{
    Invited = 0,
    Active,
    Locked,
    Suspended,
    Disabled,
    Archived,
}
