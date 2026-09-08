using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents an extension applied to the current Contract Period. Source:
/// docs/04-modules/employment/domain/entities.md, ContractExtension. A child Entity
/// of <see cref="EmploymentContract"/>, never an Aggregate Root of its own; its
/// constructor is <c>internal</c>.
/// </summary>
public sealed class ContractExtension : Entity<ContractExtensionId>
{
    public DateOnly PreviousEndDate { get; }

    public DateOnly NewEndDate { get; }

    public string Reason { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal ContractExtension(
        ContractExtensionId id, DateOnly previousEndDate, DateOnly newEndDate, string reason, DateTimeOffset createdAtUtc)
        : base(id)
    {
        PreviousEndDate = previousEndDate;
        NewEndDate = newEndDate;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }
}
