namespace Hris.Modules.Workflow.Application.Dtos;

/// <summary>
/// Read shape of a tenant's approval policy. There is deliberately no self-approval
/// field, because the aggregate holds none (WR-032): a DTO exposing one would
/// suggest a setting exists to toggle.
/// </summary>
public sealed record ApprovalPolicyDto(
    Guid Id,
    Guid TenantId,
    IReadOnlyList<Guid> ProcessesRequiringApproval,
    EscalationPolicyDto? DefaultEscalation,
    TimeSpan? DefaultSla,
    IReadOnlyList<ApprovalAuthorityLimitDto> AuthorityLimits,
    bool CustomDefinitionsPermitted,
    Guid? LastConfiguredBy,
    DateTimeOffset? LastConfiguredOn,
    string? LastConfigurationReason,
    DateTimeOffset CreatedOn);

public sealed record ApprovalAuthorityLimitDto(string RoleName, Guid? ScopeId, decimal MaximumAmount);
