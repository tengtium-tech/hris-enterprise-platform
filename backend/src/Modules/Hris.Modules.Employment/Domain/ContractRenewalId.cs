using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="ContractRenewal"/> child Entity of
/// <see cref="EmploymentContract"/>. Source:
/// docs/04-modules/employment/domain/entities.md, ContractRenewal Identity
/// ("ContractRenewalId").
/// </summary>
public readonly record struct ContractRenewalId(Guid Value) : IStronglyTypedId;
