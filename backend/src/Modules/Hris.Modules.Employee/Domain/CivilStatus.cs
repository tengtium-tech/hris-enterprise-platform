using System.Diagnostics.CodeAnalysis;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/employee-profile.md's Personal
/// Information section ("Civil Status") -- affects statutory reporting and
/// benefits/dependant eligibility, per employee-contact-information.md's
/// dependants discussion.
/// </summary>
public enum CivilStatus
{
    [SuppressMessage(
        "Naming",
        "CA1720:Identifier contains type name",
        Justification = "\"Single\" is the correct, unambiguous business term for this civil status value "
            + "-- it happens to also be a .NET type name (System.Single), which has no bearing on its meaning here.")]
    Single = 0,
    Married = 1,
    Widowed = 2,
    Separated = 3,
    Divorced = 4,
}
