namespace Hris.Modules.Administration.Application.Dtos;

public sealed record AdministrativeDelegationDto(
    Guid Id,
    Guid TenantId,
    Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId,
    IReadOnlyList<DelegatedAuthorityItemDto> DelegatedAuthority,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Reason,
    Guid? ApprovalReference,
    string Status,
    DateTimeOffset CreatedOn);

public sealed record DelegatedAuthorityItemDto(string Role, string ScopeLevel, Guid? ScopeTargetId);
