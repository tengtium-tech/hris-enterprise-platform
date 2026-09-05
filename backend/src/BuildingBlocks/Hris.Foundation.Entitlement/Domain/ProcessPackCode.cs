namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// The closed Process Pack vocabulary, per
/// docs/00-project/business-process-packs.md (DOC-014) Section 4's own catalogue --
/// entitlement-framework.md's own Pack Catalog section names these twenty-one codes
/// exactly. A tenant cannot invent a new pack; it can only hold or not hold one of
/// these. <see cref="ProcessPackCatalog"/> carries each code's Core/Optional
/// category and display name.
/// </summary>
public enum ProcessPackCode
{
    // Core -- DOC-014 Section 4.1. Always active; never subject to entitlement
    // (CTR-ENT-008). See ProcessPackCatalog.IsCore.
    Organization = 0,
    Employee = 1,
    Employment = 2,
    SelfServiceBasic = 3,
    AdministrationCore = 4,
    ComplianceBaseline = 5,
    ReportingBaseline = 6,

    // Optional -- DOC-014 Section 4.2. Entitled per the tenant's edition, or any
    // addition beyond it (Administration module, Phase 2).
    TimeAndAttendance = 7,
    Leave = 8,
    Payroll = 9,
    Benefits = 10,
    Recruitment = 11,
    Onboarding = 12,
    Performance = 13,
    Succession = 14,
    Learning = 15,
    EmployeeRelations = 16,
    OffboardingAndClearance = 17,
    Analytics = 18,
    Automation = 19,
    DeveloperPlatform = 20,
}
