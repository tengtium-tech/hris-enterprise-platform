namespace Hris.Modules.Workflow.Application.Dtos;

/// <summary>
/// Read shape of an approval delegation. Both identities are always carried, never
/// the delegate alone: a record naming only the acting delegate describes a decision
/// made by someone with no standing to make it on their own authority.
/// </summary>
public sealed record ApprovalDelegationDto(
    Guid Id,
    Guid TenantId,
    Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId,
    IReadOnlyList<Guid> Scope,
    bool CoversAllProcesses,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Reason,
    Guid? ApprovalReference,
    string Status,
    Guid CreatedBy,
    DateTimeOffset CreatedOn,
    Guid? RevokedBy,
    DateTimeOffset? RevokedOn);
