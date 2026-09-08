using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents a single renewal of an Employment Contract. Source:
/// docs/04-modules/employment/domain/entities.md, ContractRenewal. A child Entity
/// of <see cref="EmploymentContract"/>, never an Aggregate Root of its own; its
/// constructor is <c>internal</c>.
/// </summary>
public sealed class ContractRenewal : Entity<ContractRenewalId>
{
    public DateOnly PreviousStartDate { get; }

    public DateOnly? PreviousEndDate { get; }

    public DateOnly NewStartDate { get; }

    public DateOnly? NewEndDate { get; }

    public string? ApprovalReference { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal ContractRenewal(
        ContractRenewalId id,
        DateOnly previousStartDate,
        DateOnly? previousEndDate,
        DateOnly newStartDate,
        DateOnly? newEndDate,
        string? approvalReference,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PreviousStartDate = previousStartDate;
        PreviousEndDate = previousEndDate;
        NewStartDate = newStartDate;
        NewEndDate = newEndDate;
        ApprovalReference = approvalReference;
        CreatedAtUtc = createdAtUtc;
    }
}
