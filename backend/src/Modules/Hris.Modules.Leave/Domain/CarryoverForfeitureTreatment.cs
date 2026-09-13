namespace Hris.Modules.Leave.Domain;

/// <summary>What happens to balance above a <see cref="CarryoverRule.MaximumCarryover"/>.</summary>
public enum CarryoverForfeitureTreatment
{
    Forfeit,
    Encash,
}
