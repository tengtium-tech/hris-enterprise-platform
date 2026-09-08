using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents the base compensation figure in effect for an Employment over a
/// period of time. Source: docs/04-modules/employment/domain/entities.md,
/// CompensationRecord. Every prior record is preserved as an immutable historical
/// fact rather than overwritten (EMP-007) -- <see cref="Close"/> only sets
/// <see cref="EffectiveEndDate"/>, it never deletes or edits the record's own
/// figure. A child Entity of <see cref="Employment"/>, never an Aggregate Root of
/// its own; its constructor and mutating methods are <c>internal</c>.
/// </summary>
public sealed class CompensationRecord : Entity<CompensationRecordId>
{
    // Amount is assigned via object-initializer in the factory method, never through
    // this constructor: EF Core's ConstructorBindingConvention refuses to bind any
    // constructor parameter to a navigation property, and CompensationAmount is
    // mapped as an OwnsOne navigation (multiple columns: amount, currency, basis) --
    // the identical WorkLocation.Address / LegalEntity.RegisteredAddress fix from
    // Organization's own build (see memory feedback-ef-core-constructor-binding).
    public CompensationAmount Amount { get; internal set; } = null!;

    public DateOnly EffectiveStartDate { get; private set; }

    public DateOnly? EffectiveEndDate { get; private set; }

    public CompensationChangeSource ChangeSource { get; }

    public string? ApprovalReference { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal CompensationRecord(
        CompensationRecordId id,
        DateOnly effectiveStartDate,
        CompensationChangeSource changeSource,
        string? approvalReference,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        EffectiveStartDate = effectiveStartDate;
        ChangeSource = changeSource;
        ApprovalReference = approvalReference;
        CreatedAtUtc = createdAtUtc;
    }

    internal void Close(DateOnly endDate)
    {
        EffectiveEndDate = endDate;
    }
}
