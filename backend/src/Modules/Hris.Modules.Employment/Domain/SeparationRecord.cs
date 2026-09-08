using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents the details of an employment separation. Source:
/// docs/04-modules/employment/domain/entities.md, SeparationRecord. A child Entity
/// of <see cref="Employment"/>, never an Aggregate Root of its own; its constructor
/// is <c>internal</c>, reachable only through <see cref="Employment"/>.
/// </summary>
public sealed class SeparationRecord : Entity<SeparationRecordId>
{
    public SeparationType SeparationType { get; }

    public TerminationReason? TerminationReason { get; }

    public DateOnly LastWorkingDate { get; }

    public DateOnly EffectiveSeparationDate { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal SeparationRecord(
        SeparationRecordId id,
        SeparationType separationType,
        TerminationReason? terminationReason,
        DateOnly lastWorkingDate,
        DateOnly effectiveSeparationDate,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        SeparationType = separationType;
        TerminationReason = terminationReason;
        LastWorkingDate = lastWorkingDate;
        EffectiveSeparationDate = effectiveSeparationDate;
        CreatedAtUtc = createdAtUtc;
    }
}
