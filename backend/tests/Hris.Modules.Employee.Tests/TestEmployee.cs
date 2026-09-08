using Hris.Modules.Employee.Domain;

namespace Hris.Modules.Employee.Tests;

/// <summary>
/// Shared aggregate-construction helper for Domain and Application-layer handler
/// tests, the same role <c>TestEmployment</c> plays for
/// <c>Hris.Modules.Employment.Tests</c>.
/// </summary>
internal static class TestEmployee
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Hris.Modules.Employee.Domain.Employee Create(Guid tenantId) =>
        Hris.Modules.Employee.Domain.Employee.Create(
            new EmployeeId(Guid.NewGuid()), tenantId, "EMP-000001", "Juan", "Santos", "Dela Cruz", null, null, null,
            new DateOnly(1990, 1, 1), "Manila", Gender.Male, CivilStatus.Single, "Filipino", "Philippine", NowUtc).Value;

    public static Hris.Modules.Employee.Domain.Employee CreateActive(Guid tenantId)
    {
        var employee = Create(tenantId);
        employee.StartOnboarding(NowUtc);
        employee.Activate(NowUtc);
        return employee;
    }

    public static Hris.Modules.Employee.Domain.Employee CreateSeparated(Guid tenantId)
    {
        var employee = CreateActive(tenantId);
        employee.StartOffboarding(NowUtc);
        employee.Separate(NowUtc);
        return employee;
    }
}
