namespace Hris.Modules.Leave.Domain;

/// <summary>Which statutory leave type's specific conditions a <see cref="StatutoryDetails"/> instance carries.</summary>
public enum StatutoryDetailsVariant
{
    Maternity,
    Paternity,
    SoloParent,
    Vawc,
    SpecialLeaveBenefit,
}
