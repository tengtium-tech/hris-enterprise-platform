using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for a <see cref="LeaveLedgerEntry"/> entity.</summary>
public readonly record struct LeaveLedgerEntryId(Guid Value) : IStronglyTypedId;
