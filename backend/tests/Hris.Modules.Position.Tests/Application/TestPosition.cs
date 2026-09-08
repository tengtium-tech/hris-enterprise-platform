using Hris.Modules.Position.Domain;

namespace Hris.Modules.Position.Tests.Application;

/// <summary>
/// Shared aggregate-construction helper for Application-layer handler tests, the
/// same role <c>TestOrganization</c> plays for <c>Hris.Modules.Organization.Tests</c>.
/// </summary>
internal static class TestPosition
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Hris.Modules.Position.Domain.Position Create(Guid tenantId) => Hris.Modules.Position.Domain.Position.Create(
        new PositionId(Guid.NewGuid()), tenantId, "POS-000001", "Senior Software Engineer", "Individual Contributor",
        Guid.NewGuid(), null, null, null, null, null, null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        null, 1, NowUtc).Value;

    public static JobFamily CreateJobFamily(Guid tenantId) =>
        JobFamily.Create(new JobFamilyId(Guid.NewGuid()), tenantId, "IT", "Information Technology", null, NowUtc).Value;

    public static JobClassification CreateJobClassification(Guid tenantId) =>
        JobClassification.Create(new JobClassificationId(Guid.NewGuid()), tenantId, "PROF", "Professional", null, NowUtc).Value;

    public static JobGrade CreateJobGrade(Guid tenantId) =>
        JobGrade.Create(new JobGradeId(Guid.NewGuid()), tenantId, "G08", "Grade 8", null, 8, NowUtc).Value;
}
