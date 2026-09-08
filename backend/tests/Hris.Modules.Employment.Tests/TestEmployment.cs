using Hris.Modules.Employment.Domain;

namespace Hris.Modules.Employment.Tests;

/// <summary>
/// Shared aggregate-construction helper for Domain and Application-layer handler
/// tests, the same role <c>TestPosition</c> plays for
/// <c>Hris.Modules.Position.Tests</c>.
/// </summary>
internal static class TestEmployment
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Hris.Modules.Employment.Domain.Employment Create(Guid tenantId, Guid? employeeId = null) =>
        Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), tenantId, employeeId ?? Guid.NewGuid(), "EMP-000001", "Regular",
            "Rank-and-File", true, null, null, true, false, NowUtc).Value;

    public static Hris.Modules.Employment.Domain.Employment CreateActive(Guid tenantId, Guid? employeeId = null)
    {
        var employment = Create(tenantId, employeeId);
        employment.Activate(true, true, NowUtc);
        return employment;
    }

    public static EmploymentContract CreateContract(Guid tenantId, Guid employmentId) =>
        EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), tenantId, employmentId, "Regular",
            DateOnly.FromDateTime(NowUtc.UtcDateTime), null, false, null, NowUtc).Value;

    public static EmploymentAssignment CreateAssignment(Guid tenantId, Guid employmentId) =>
        EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), tenantId, employmentId, Guid.NewGuid(), Guid.NewGuid(), null,
            null, null, null, WorkArrangement.OnSite, null, DateOnly.FromDateTime(NowUtc.UtcDateTime), true, NowUtc).Value;
}
