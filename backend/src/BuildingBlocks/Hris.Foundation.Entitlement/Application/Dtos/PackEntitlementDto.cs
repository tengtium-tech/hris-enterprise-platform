namespace Hris.Foundation.Entitlement.Application.Dtos;

/// <summary>
/// One row of <c>GetEditionEntitlementSummaryQuery</c>'s own result -- one Process
/// Pack's own entitlement standing for the queried edition. <see cref="MaturityLevel"/>
/// is <c>null</c> whenever <see cref="IsEntitled"/> is <c>false</c>, matching
/// tenant-configuration.md's own "GetTenantQuery ... Process Pack entitlement
/// summary" reference.
/// </summary>
public sealed record PackEntitlementDto(
    string Code,
    string DisplayName,
    bool IsCore,
    bool IsEntitled,
    string? MaturityLevel);
