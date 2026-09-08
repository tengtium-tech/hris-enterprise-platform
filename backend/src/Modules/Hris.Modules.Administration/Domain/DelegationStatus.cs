namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Source: docs/04-modules/administration/domain/delegated-administration.md's
/// "Lifecycle" diagram: Scheduled -&gt; Active -&gt; Expired, or Scheduled/Active -&gt;
/// Revoked (terminal).
/// </summary>
public enum DelegationStatus
{
    Scheduled = 0,
    Active = 1,
    Expired = 2,
    Revoked = 3,
}
