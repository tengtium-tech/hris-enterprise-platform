namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// The Process Pack catalogue, per DOC-014 Section 4 -- static platform vocabulary,
/// not a database row (entitlement-framework.md's own Scope section states why).
/// The seven Core packs are always entitled (CTR-ENT-008); the fourteen Optional
/// packs are entitled per <see cref="EditionDefaultPackComposition"/>, or per a
/// per-tenant override Administration module's own ProcessPackActivation grants
/// beyond it (Phase 2, out of this framework's own scope).
/// </summary>
public static class ProcessPackCatalog
{
    private static readonly HashSet<ProcessPackCode> _corePacks = new()
    {
        ProcessPackCode.Organization,
        ProcessPackCode.Employee,
        ProcessPackCode.Employment,
        ProcessPackCode.SelfServiceBasic,
        ProcessPackCode.AdministrationCore,
        ProcessPackCode.ComplianceBaseline,
        ProcessPackCode.ReportingBaseline,
    };

    private static readonly Dictionary<ProcessPackCode, string> _displayNames = new()
    {
        [ProcessPackCode.Organization] = "Organization",
        [ProcessPackCode.Employee] = "Employee",
        [ProcessPackCode.Employment] = "Employment",
        [ProcessPackCode.SelfServiceBasic] = "Self-Service (Basic)",
        [ProcessPackCode.AdministrationCore] = "Administration",
        [ProcessPackCode.ComplianceBaseline] = "Compliance Baseline",
        [ProcessPackCode.ReportingBaseline] = "Reporting Baseline",
        [ProcessPackCode.TimeAndAttendance] = "Time & Attendance",
        [ProcessPackCode.Leave] = "Leave",
        [ProcessPackCode.Payroll] = "Payroll",
        [ProcessPackCode.Benefits] = "Benefits",
        [ProcessPackCode.Recruitment] = "Recruitment",
        [ProcessPackCode.Onboarding] = "Onboarding",
        [ProcessPackCode.Performance] = "Performance",
        [ProcessPackCode.Succession] = "Succession",
        [ProcessPackCode.Learning] = "Learning",
        [ProcessPackCode.EmployeeRelations] = "Employee Relations",
        [ProcessPackCode.OffboardingAndClearance] = "Offboarding & Clearance",
        [ProcessPackCode.Analytics] = "Analytics",
        [ProcessPackCode.Automation] = "Automation",
        [ProcessPackCode.DeveloperPlatform] = "Developer Platform",
    };

    /// <summary>
    /// DOC-014 Section 5's own dependency table. Carried as reference data only --
    /// entitlement-framework.md's own Pack Dependencies section states that
    /// validating a conditional dependency happens when a pack is actually activated,
    /// which is Administration module's own future command, not this catalogue's own
    /// concern.
    /// </summary>
    private static readonly Dictionary<ProcessPackCode, IReadOnlyCollection<ProcessPackCode>> _conditionalDependencies =
        new()
        {
            [ProcessPackCode.Payroll] = new[] { ProcessPackCode.TimeAndAttendance, ProcessPackCode.Leave },
            [ProcessPackCode.OffboardingAndClearance] = new[] { ProcessPackCode.Payroll },
        };

    public static bool IsCore(ProcessPackCode pack) => _corePacks.Contains(pack);

    public static string GetDisplayName(ProcessPackCode pack) => _displayNames[pack];

    public static IReadOnlyCollection<ProcessPackCode> GetConditionalDependencies(ProcessPackCode pack) =>
        _conditionalDependencies.TryGetValue(pack, out var dependencies)
            ? dependencies
            : Array.Empty<ProcessPackCode>();

    public static IReadOnlyCollection<ProcessPackCode> AllPacks { get; } = Enum.GetValues<ProcessPackCode>();
}
