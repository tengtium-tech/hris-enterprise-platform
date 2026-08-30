using FluentAssertions;
using Hris.Foundation.Configuration.Domain;
using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-DAT-001 through CTR-DAT-006, docs/09-testing/critical-test-requirements.md §13.
/// Meaningful once at least one Aggregate Root has a real EF Core persistence
/// implementation (Phase 2 onward).
/// </summary>
public class DataIntegrityTests
{
    [Fact(Skip = "Not yet implemented. CTR-DAT-001 — Concurrent Modification Is Detected.")]
    public void CTR_DAT_001_ConcurrentModificationIsDetected()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-DAT-002 — Aggregate Invariants Cannot Be Bypassed.")]
    public void CTR_DAT_002_AggregateInvariantsCannotBeBypassed()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-DAT-003 — Restoration From Backup Succeeds.")]
    public void CTR_DAT_003_RestorationFromBackupSucceeds()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-DAT-004 — Soft-Deleted Records Are Excluded by Default.")]
    public void CTR_DAT_004_SoftDeletedRecordsAreExcludedByDefault()
    {
    }

    /// <summary>
    /// Exercises <see cref="ConfigurationSetting.GetValueAsOf"/> directly against the
    /// Domain layer -- no persistence needed, since this CTR is about the Aggregate's
    /// own effective-dating logic, not about EF Core round-tripping it. Two versions,
    /// both taken all the way to Published (the minimum state
    /// <see cref="ConfigurationSetting.GetValueAsOf"/> considers "in force" per that
    /// method's own remarks), with different effective dates -- the test asserts the
    /// query answers "which value applies on this date" purely from stored dates, per
    /// configuration-framework.md's own Implementation Guidance: "Effective-date
    /// configuration where a change must not alter historical computation."
    /// </summary>
    [Fact]
    public void CTR_DAT_005_EffectiveDatedConfigurationSelectsTheVersionInForceOnTheEvaluationDate()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var v1EffectiveDate = new DateOnly(2026, 1, 1);
        var v2EffectiveDate = new DateOnly(2026, 6, 1);

        var settingResult = ConfigurationSetting.Create(
            ConfigurationKey.Create("Payroll.GracePeriodMinutes").Value,
            ConfigurationScope.Global(),
            ConfigurationCategory.Payroll,
            ConfigurationDataType.Number,
            initialValue: "10",
            effectiveDate: v1EffectiveDate,
            expirationDate: null,
            changeSummary: "Initial grace period.",
            createdByUserId: userId,
            nowUtc: now);

        settingResult.IsSuccess.Should().BeTrue();
        var setting = settingResult.Value;
        var v1Id = setting.Versions[0].Id;

        PublishThroughLifecycle(setting, v1Id, now);

        var draftResult = setting.CreateNewDraftVersion(
            value: "15",
            effectiveDate: v2EffectiveDate,
            expirationDate: null,
            changeSummary: "Increased grace period.",
            createdByUserId: userId,
            nowUtc: now);

        draftResult.IsSuccess.Should().BeTrue();
        var v2Id = draftResult.Value.Id;

        PublishThroughLifecycle(setting, v2Id, now);

        // Before either version's effective date: nothing is in force yet.
        setting.GetValueAsOf(new DateOnly(2025, 12, 31)).IsFailure.Should().BeTrue();

        // On and after v1's effective date, before v2's: v1's value applies.
        setting.GetValueAsOf(v1EffectiveDate).Value.Should().Be("10");
        setting.GetValueAsOf(new DateOnly(2026, 3, 15)).Value.Should().Be("10");

        // On and after v2's effective date: v2's value applies, even though v1 is
        // still sitting in the Published state and nobody has called Activate() on
        // v2 -- exactly the distinction this class's own ConfigurationVersion.IsInForceOn
        // remarks describe: "never from ... its current State beyond that threshold."
        setting.GetValueAsOf(v2EffectiveDate).Value.Should().Be("15");
        setting.GetValueAsOf(new DateOnly(2026, 12, 31)).Value.Should().Be("15");

        // Re-running the same historical query later must keep returning the same
        // answer -- the whole point of effective-dated configuration
        // (configuration-framework.md: "a change must not alter historical
        // computation").
        setting.GetValueAsOf(v1EffectiveDate).Value.Should().Be("10");
    }

    private static void PublishThroughLifecycle(ConfigurationSetting setting, ConfigurationVersionId versionId, DateTimeOffset now)
    {
        setting.ValidateVersion(versionId, now).IsSuccess.Should().BeTrue();
        setting.ApproveVersion(versionId, Guid.NewGuid(), now).IsSuccess.Should().BeTrue();
        setting.PublishVersion(versionId, now).IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "Not yet implemented. CTR-DAT-006 — Recalculation Reproduces Identical Results From Identical Inputs.")]
    public void CTR_DAT_006_RecalculationReproducesIdenticalResultsFromIdenticalInputs()
    {
    }
}
