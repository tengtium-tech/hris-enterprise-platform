using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identity of the <see cref="AdministrativeDelegation"/> Aggregate Root.
/// </summary>
public readonly record struct DelegationId(Guid Value) : IStronglyTypedId;
