using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

public sealed class LegalEntityTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Succeeds_WithValidData()
    {
        var result = LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", "TengTium Software Inc.",
            "REG-12345", null, "Philippines", null, null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.BusinessRegistrationNumber.Value.Should().Be("REG-12345");
        result.Value.DomainEvents.Should().ContainSingle(e => e is LegalEntityCreated);
    }

    [Fact]
    public void Create_Fails_WhenBusinessRegistrationNumberIsMissing()
    {
        var result = LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, null, null,
            "Philippines", null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.BusinessRegistrationNumberRequired);
    }

    [Fact]
    public void Update_Succeeds_WhenActive()
    {
        var legalEntity = CreateLegalEntity();

        var result = legalEntity.Update("New Name", null, null, "Philippines", null, null, _now);

        result.IsSuccess.Should().BeTrue();
        legalEntity.Name.Should().Be("New Name");
    }

    [Fact]
    public void Archive_ThenUpdate_Fails()
    {
        var legalEntity = CreateLegalEntity();
        legalEntity.Archive(_now);

        var result = legalEntity.Update("New Name", null, null, "Philippines", null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void Archive_Fails_WhenAlreadyArchived()
    {
        var legalEntity = CreateLegalEntity();
        legalEntity.Archive(_now);

        var result = legalEntity.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    private static LegalEntity CreateLegalEntity() =>
        LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, "REG-12345", null,
            "Philippines", null, null, _now).Value;
}
