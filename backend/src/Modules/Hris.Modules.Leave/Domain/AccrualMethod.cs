namespace Hris.Modules.Leave.Domain;

/// <summary>How balance accrues for the leave type an <see cref="AccrualRule"/> configures.</summary>
public enum AccrualMethod
{
    /// <summary>Full entitlement granted at eligibility, in one step.</summary>
    Immediate,

    /// <summary>Accrued periodically per <see cref="AccrualFrequency"/>.</summary>
    Periodic,

    /// <summary>Granted at a service-length trigger.</summary>
    Milestone,
}
