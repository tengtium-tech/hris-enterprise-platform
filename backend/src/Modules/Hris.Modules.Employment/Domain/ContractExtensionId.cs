using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="ContractExtension"/> child Entity of
/// <see cref="EmploymentContract"/>. Source:
/// docs/04-modules/employment/domain/entities.md, ContractExtension Identity
/// ("ContractExtensionId").
/// </summary>
public readonly record struct ContractExtensionId(Guid Value) : IStronglyTypedId;
