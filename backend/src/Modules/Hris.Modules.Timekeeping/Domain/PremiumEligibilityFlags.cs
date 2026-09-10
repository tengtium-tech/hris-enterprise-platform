using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Which premiums a shift makes an employee eligible for. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// Flags only. There is deliberately no rate, percentage, or amount field, and none
/// should ever be added: the peso value of a night differential or holiday premium
/// is <c>payroll</c> and <c>compensation</c>'s to define, computed against the flag
/// this module states. That boundary is the reason this type looks thinner than it
/// first appears it should.
/// </summary>
public sealed class PremiumEligibilityFlags : ValueObject
{
    public bool NightDifferentialEligible { get; }

    public bool HazardEligible { get; }

    public bool HolidayPremiumEligible { get; }

    private PremiumEligibilityFlags(bool nightDifferentialEligible, bool hazardEligible, bool holidayPremiumEligible)
    {
        NightDifferentialEligible = nightDifferentialEligible;
        HazardEligible = hazardEligible;
        HolidayPremiumEligible = holidayPremiumEligible;
    }

    public static PremiumEligibilityFlags Create(
        bool nightDifferentialEligible, bool hazardEligible, bool holidayPremiumEligible) =>
        new(nightDifferentialEligible, hazardEligible, holidayPremiumEligible);

    public static PremiumEligibilityFlags None => new(false, false, false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NightDifferentialEligible;
        yield return HazardEligible;
        yield return HolidayPremiumEligible;
    }
}
