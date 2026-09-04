using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryReferenceDataEventsTests
{
    [Fact]
    public void Publish_RaisesExactlyOneEvent_CarryingTheExpectedFields()
    {
        var version = TestData.PublishedVersion();

        var raised = version.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StatutoryTableVersionPublished>().Subject;

        raised.StatutoryTableVersionId.Should().Be(version.Id);
        raised.StatutoryProgramId.Should().Be(version.StatutoryProgramId);
        raised.VersionLabel.Should().Be(version.VersionLabel.Value);
        raised.EffectiveFromUtc.Should().Be(version.EffectiveFromUtc);
    }

    [Fact]
    public void RecordSignoff_RaisesExactlyOneAdditionalEvent_CarryingTheExpectedFields()
    {
        var version = TestData.PublishedVersion();

        version.RecordSignoff(TestData.NowUtc, "Reviewer Name");

        var raised = version.DomainEvents.OfType<StatutoryTableVersionSignedOff>().Should().ContainSingle().Subject;
        raised.StatutoryTableVersionId.Should().Be(version.Id);
        raised.SignoffDateUtc.Should().Be(TestData.NowUtc);
        raised.SignoffBy.Should().Be("Reviewer Name");
        version.DomainEvents.Should().HaveCount(2, "one from Publish, one from RecordSignoff");
    }
}
