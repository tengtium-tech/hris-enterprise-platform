namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The seven named groupings under configuration-framework.md's Configuration
/// Categories section (Platform, Organization, Payroll, Attendance, Leave,
/// Notification, Security Configuration). Descriptive only -- no behavior attaches to
/// a category, so this is a Simple Enumeration per enumeration-pattern.md.
/// </summary>
public enum ConfigurationCategory
{
    Platform = 0,
    Organization,
    Payroll,
    Attendance,
    Leave,
    Notification,
    Security,
}
