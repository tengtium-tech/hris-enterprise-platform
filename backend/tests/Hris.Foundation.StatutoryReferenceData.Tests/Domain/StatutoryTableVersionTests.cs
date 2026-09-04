using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryTableVersionTests
{
    [Fact]
    public void Publish_Succeeds_AndRaisesStatutoryTableVersionPublished()
    {
        var programId = new StatutoryProgramId(Guid.NewGuid());
        var versionLabel = TestData.NewVersionLabel();
        var provenance = TestData.NewProvenance();

        var result = StatutoryTableVersion.Publish(
            programId, versionLabel, TestData.NowUtc, null, provenance, TestData.NewScheduleData(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.StatutoryProgramId.Should().Be(programId);
        result.Value.VersionLabel.Should().Be(versionLabel);
        result.Value.EffectiveFromUtc.Should().Be(TestData.NowUtc);
        result.Value.EffectiveToUtc.Should().BeNull();
        result.Value.Provenance.SignoffStatus.Should().Be(StatutorySignoffStatus.PendingHumanSignoff);
        result.Value.DomainEvents.OfType<StatutoryTableVersionPublished>().Should().ContainSingle();
    }

    [Fact]
    public void Publish_Succeeds_WhenEffectiveFromIsInTheFuture()
    {
        var futureDate = TestData.NowUtc.AddMonths(1);

        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), futureDate, null,
            TestData.NewProvenance(), TestData.NewScheduleData(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue("Update Lifecycle Requirement 3 allows publishing ahead of the effective date");
    }

    [Fact]
    public void Publish_Fails_WhenEffectiveToPrecedesEffectiveFrom()
    {
        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), TestData.NowUtc,
            TestData.NowUtc.AddDays(-1), TestData.NewProvenance(), TestData.NewScheduleData(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.EffectiveToBeforeEffectiveFrom);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Publish_Fails_WhenScheduleDataIsMissing(string? scheduleData)
    {
        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), TestData.NowUtc, null,
            TestData.NewProvenance(), scheduleData, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ScheduleDataRequired);
    }

    [Fact]
    public void Publish_Fails_WhenScheduleDataIsNotValidJson()
    {
        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), TestData.NowUtc, null,
            TestData.NewProvenance(), "{not valid json", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ScheduleDataMustBeValidJson);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Publish_Fails_WhenIssuingAuthorityIsMissing(string? issuingAuthority)
    {
        var provenance = TestData.NewProvenance() with { IssuingAuthority = issuingAuthority! };

        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), TestData.NowUtc, null,
            provenance, TestData.NewScheduleData(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.IssuingAuthorityRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Publish_Fails_WhenIssuanceReferenceIsMissing(string? issuanceReference)
    {
        var provenance = TestData.NewProvenance() with { IssuanceReference = issuanceReference! };

        var result = StatutoryTableVersion.Publish(
            new StatutoryProgramId(Guid.NewGuid()), TestData.NewVersionLabel(), TestData.NowUtc, null,
            provenance, TestData.NewScheduleData(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.IssuanceReferenceRequired);
    }

    [Fact]
    public void RecordSignoff_Succeeds_AndRaisesStatutoryTableVersionSignedOff()
    {
        var version = TestData.PublishedVersion();

        var result = version.RecordSignoff(TestData.NowUtc, "Reviewer Name");

        result.IsSuccess.Should().BeTrue();
        version.Provenance.SignoffStatus.Should().Be(StatutorySignoffStatus.SignedOff);
        version.Provenance.SignoffDateUtc.Should().Be(TestData.NowUtc);
        version.Provenance.SignoffBy.Should().Be("Reviewer Name");
        version.DomainEvents.OfType<StatutoryTableVersionSignedOff>().Should().ContainSingle();
    }

    [Fact]
    public void RecordSignoff_DoesNotChange_TheSubstantiveScheduleData()
    {
        var version = TestData.PublishedVersion();
        var originalScheduleData = version.ScheduleData;

        version.RecordSignoff(TestData.NowUtc, "Reviewer Name");

        version.ScheduleData.Should().Be(originalScheduleData, "Update Lifecycle Requirement 1: existing versions are never edited");
    }

    [Fact]
    public void RecordSignoff_Fails_WhenAlreadySignedOff()
    {
        var version = TestData.SignedOffVersion();

        var result = version.RecordSignoff(TestData.NowUtc, "Second Reviewer");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.AlreadySignedOff);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSignoff_Fails_WhenSignoffByIsMissing(string? signoffBy)
    {
        var version = TestData.PublishedVersion();

        var result = version.RecordSignoff(TestData.NowUtc, signoffBy);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.SignoffByRequired);
    }
}
