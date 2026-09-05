namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// docs/00-project/business-model.md (DOC-011) Section 4.1's own "Packs by Edition"
/// table, translated into queryable data -- entitlement-framework.md's own Edition
/// Default Composition section reproduces this same table for a human reader. Core
/// packs are omitted here deliberately: <see cref="EntitlementEvaluator"/> never
/// consults this table for a Core pack, since Core is always entitled regardless of
/// edition (CTR-ENT-008).
/// </summary>
public static class EditionDefaultPackComposition
{
    private static readonly Dictionary<TenantEditionCode, IReadOnlyDictionary<ProcessPackCode, MaturityLevel>> _composition =
        new()
        {
            [TenantEditionCode.Starter] = new Dictionary<ProcessPackCode, MaturityLevel>
            {
                [ProcessPackCode.TimeAndAttendance] = MaturityLevel.Essential,
                [ProcessPackCode.Leave] = MaturityLevel.Essential,
                [ProcessPackCode.Payroll] = MaturityLevel.Essential,
            },
            [TenantEditionCode.Growth] = new Dictionary<ProcessPackCode, MaturityLevel>
            {
                [ProcessPackCode.TimeAndAttendance] = MaturityLevel.Standard,
                [ProcessPackCode.Leave] = MaturityLevel.Standard,
                [ProcessPackCode.Payroll] = MaturityLevel.Standard,
                [ProcessPackCode.Benefits] = MaturityLevel.Essential,
                [ProcessPackCode.Recruitment] = MaturityLevel.Standard,
                [ProcessPackCode.Onboarding] = MaturityLevel.Standard,
                [ProcessPackCode.Performance] = MaturityLevel.Standard,
                [ProcessPackCode.OffboardingAndClearance] = MaturityLevel.Essential,
            },
            [TenantEditionCode.Enterprise] = new Dictionary<ProcessPackCode, MaturityLevel>
            {
                [ProcessPackCode.TimeAndAttendance] = MaturityLevel.Advanced,
                [ProcessPackCode.Leave] = MaturityLevel.Advanced,
                [ProcessPackCode.Payroll] = MaturityLevel.Advanced,
                [ProcessPackCode.Benefits] = MaturityLevel.Advanced,
                [ProcessPackCode.Recruitment] = MaturityLevel.Advanced,
                [ProcessPackCode.Onboarding] = MaturityLevel.Advanced,
                [ProcessPackCode.Performance] = MaturityLevel.Advanced,
                [ProcessPackCode.OffboardingAndClearance] = MaturityLevel.Advanced,
                [ProcessPackCode.Learning] = MaturityLevel.Standard,
                [ProcessPackCode.Succession] = MaturityLevel.Standard,
                [ProcessPackCode.EmployeeRelations] = MaturityLevel.Standard,
                [ProcessPackCode.Analytics] = MaturityLevel.Advanced,
                [ProcessPackCode.Automation] = MaturityLevel.Advanced,
                [ProcessPackCode.DeveloperPlatform] = MaturityLevel.Advanced,
            },
            [TenantEditionCode.Government] = new Dictionary<ProcessPackCode, MaturityLevel>
            {
                [ProcessPackCode.TimeAndAttendance] = MaturityLevel.Advanced,
                [ProcessPackCode.Leave] = MaturityLevel.Advanced,
                [ProcessPackCode.Payroll] = MaturityLevel.Advanced,
                [ProcessPackCode.Benefits] = MaturityLevel.Advanced,
                [ProcessPackCode.Recruitment] = MaturityLevel.Advanced,
                [ProcessPackCode.Onboarding] = MaturityLevel.Advanced,
                [ProcessPackCode.Performance] = MaturityLevel.Advanced,
                [ProcessPackCode.OffboardingAndClearance] = MaturityLevel.Advanced,
                [ProcessPackCode.Learning] = MaturityLevel.Standard,
                [ProcessPackCode.Succession] = MaturityLevel.Standard,
                [ProcessPackCode.EmployeeRelations] = MaturityLevel.Advanced,
                [ProcessPackCode.Analytics] = MaturityLevel.Advanced,
                [ProcessPackCode.Automation] = MaturityLevel.Advanced,
                [ProcessPackCode.DeveloperPlatform] = MaturityLevel.Advanced,
            },
        };

    /// <summary>
    /// The maturity level an Optional pack is held at by default for the given
    /// edition, or <c>null</c> if that edition's own default composition does not
    /// include the pack at all (DOC-011 Section 4.1's own dash entries).
    /// </summary>
    public static MaturityLevel? TryGetDefaultMaturityLevel(TenantEditionCode edition, ProcessPackCode pack) =>
        _composition[edition].TryGetValue(pack, out var maturityLevel) ? maturityLevel : null;

    /// <summary>
    /// Every Optional pack the given edition holds by default, with its own default
    /// maturity level -- backs a future "what does this tenant have" summary.
    /// </summary>
    public static IReadOnlyDictionary<ProcessPackCode, MaturityLevel> GetDefaultComposition(TenantEditionCode edition) =>
        _composition[edition];
}
