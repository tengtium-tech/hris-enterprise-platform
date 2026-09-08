namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/employee-history.md's "Historical
/// Categories" section, narrowed to what the Employee Aggregate itself owns per
/// that same document's corrected "Employment, Organization, Position, Manager...
/// Are Owned by the Employment Module" note: Employee History records only
/// Lifecycle Stage transitions and changes to Employee's own current-state
/// information (personal, contact, government, banking).
/// </summary>
public enum EmployeeHistoryCategory
{
    LifecycleStage = 0,
    PersonalInformation = 1,
    ContactInformation = 2,
    GovernmentInformation = 3,
    BankingInformation = 4,
}
