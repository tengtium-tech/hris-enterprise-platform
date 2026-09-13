namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Part of a <see cref="LeavePolicyRuleset"/>: governs what happens to unused balance at a
/// configured period boundary. Consumed only by the scheduled carryover processor, never
/// evaluated ad hoc elsewhere. For a statutory leave type mandating non-forfeiture (for
/// example SIL's commutability under Labor Code Art. 95), tenant configuration cannot fall
/// below that floor (LV-063). Source: docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record CarryoverRule(
    decimal MaximumCarryover,
    int? ExpiryGraceDays,
    CarryoverForfeitureTreatment ExcessTreatment);
