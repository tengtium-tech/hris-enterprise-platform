namespace Hris.Modules.Employment.Application.Dtos;

public sealed record EmploymentContractDto(
    Guid Id,
    Guid TenantId,
    Guid EmploymentId,
    string ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    string LifecycleStage,
    Guid? SupersedesContractId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ContractRenewalDto> Renewals,
    IReadOnlyList<ContractExtensionDto> Extensions,
    IReadOnlyList<ContractDocumentDto> Documents);

public sealed record ContractRenewalDto(
    Guid Id, DateOnly PreviousStartDate, DateOnly? PreviousEndDate, DateOnly NewStartDate, DateOnly? NewEndDate,
    string? ApprovalReference);

public sealed record ContractExtensionDto(Guid Id, DateOnly PreviousEndDate, DateOnly NewEndDate, string Reason);

public sealed record ContractDocumentDto(Guid Id, string DocumentType, string StorageReference, int Version);
