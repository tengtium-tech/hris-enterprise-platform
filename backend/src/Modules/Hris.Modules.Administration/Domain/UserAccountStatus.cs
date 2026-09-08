namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Source: docs/04-modules/administration/domain/platform-users.md's "Account
/// Lifecycle" diagram: Pending -&gt; Active -&gt;/&lt;- Suspended, Active -&gt;
/// Deprovisioned (terminal), Suspended -&gt; Deprovisioned (terminal).
/// </summary>
public enum UserAccountStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Deprovisioned = 3,
}
