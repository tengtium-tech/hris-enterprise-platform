using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of a rotation-generation configuration. The rotation configuration
/// itself is not modelled in this Sprint; the identifier exists so a
/// rotation-generated <see cref="ShiftAssignment"/> can record which cycle produced
/// it, which TK-036 requires in order for such assignments to stay individually
/// auditable rather than re-derived from the rule at read time.
/// </summary>
public readonly record struct RotationCycleId(Guid Value) : IStronglyTypedId;
