using Hris.Foundation.StatutoryReferenceData.Domain;

namespace Hris.Foundation.StatutoryReferenceData.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static StatutoryProgramCode NewProgramCode(string? value = null) =>
        StatutoryProgramCode.Create(value ?? "SSS").Value;

    public static StatutoryCountryCode NewCountry(string? value = null) =>
        StatutoryCountryCode.Create(value ?? "PH").Value;

    public static StatutoryTableVersionLabel NewVersionLabel(string? value = null) =>
        StatutoryTableVersionLabel.Create(value ?? "2025-01").Value;

    public static StatutoryProgram NewProgram(
        StatutoryProgramCode? code = null,
        StatutoryCountryCode? country = null,
        string displayName = "SSS Contribution Schedule",
        DateTimeOffset? registeredAtUtc = null) =>
        StatutoryProgram.Register(
            code ?? NewProgramCode(), country ?? NewCountry(), displayName, registeredAtUtc ?? NowUtc).Value;

    public static StatutoryTableProvenance NewProvenance(
        StatutorySignoffStatus signoffStatus = StatutorySignoffStatus.PendingHumanSignoff,
        DateTimeOffset? signoffDateUtc = null,
        string? signoffBy = null) =>
        new(
            "Social Security System (SSS)",
            "SSS Circular No. 2024-006",
            NowUtc,
            StatutoryVerificationSourceType.PrimarySourceRead,
            NowUtc,
            signoffStatus,
            signoffStatus == StatutorySignoffStatus.SignedOff ? signoffDateUtc ?? NowUtc : null,
            signoffStatus == StatutorySignoffStatus.SignedOff ? signoffBy ?? "Reviewer Name" : null);

    public static string NewScheduleData() => """{"brackets":[{"min":0,"max":5249.99,"total":760.00}]}""";

    public static StatutoryTableVersion PublishedVersion(
        StatutoryProgramId? statutoryProgramId = null,
        StatutoryTableVersionLabel? versionLabel = null,
        DateTimeOffset? effectiveFromUtc = null,
        DateTimeOffset? effectiveToUtc = null,
        StatutoryTableProvenance? provenance = null,
        string? scheduleData = null,
        DateTimeOffset? publishedAtUtc = null) =>
        StatutoryTableVersion.Publish(
            statutoryProgramId ?? new StatutoryProgramId(Guid.NewGuid()),
            versionLabel ?? NewVersionLabel(),
            effectiveFromUtc ?? NowUtc,
            effectiveToUtc,
            provenance ?? NewProvenance(),
            scheduleData ?? NewScheduleData(),
            publishedAtUtc ?? NowUtc).Value;

    public static StatutoryTableVersion SignedOffVersion(
        StatutoryProgramId? statutoryProgramId = null, DateTimeOffset? effectiveFromUtc = null)
    {
        var version = PublishedVersion(statutoryProgramId, effectiveFromUtc: effectiveFromUtc);
        version.RecordSignoff(NowUtc, "Reviewer Name");
        return version;
    }
}
