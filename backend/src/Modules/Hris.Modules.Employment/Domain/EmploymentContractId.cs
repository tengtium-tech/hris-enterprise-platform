using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="EmploymentContract"/> Aggregate Root. Source:
/// docs/04-modules/employment/domain/entities.md, Employment Contract Aggregate Root
/// Identity ("EmploymentContractId").
/// </summary>
public readonly record struct EmploymentContractId(Guid Value) : IStronglyTypedId;
