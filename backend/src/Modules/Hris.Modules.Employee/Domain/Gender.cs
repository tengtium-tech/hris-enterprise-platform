namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/employee-profile.md's Personal
/// Information section ("Gender"). No further enumeration is documented; this
/// module follows the same "closed set covering the platform's own statutory
/// reporting needs, extendable if a future jurisdiction requires it" reasoning
/// already applied to Position's own WorkforceClassificationStatus.
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2,
    PreferNotToSay = 3,
}
