namespace Hris.Modules.Leave.Domain;

/// <summary>How often a <see cref="AccrualMethod.Periodic"/> <see cref="AccrualRule"/> accrues.</summary>
public enum AccrualFrequency
{
    Monthly,
    PerPayPeriod,
    Anniversary,
}
