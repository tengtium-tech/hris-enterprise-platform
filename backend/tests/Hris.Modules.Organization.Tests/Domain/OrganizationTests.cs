using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

public sealed class OrganizationTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Succeeds_WithValidNameAndCode()
    {
        var result = Hris.Modules.Organization.Domain.Organization.Create(
            new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "corp", null, "desc", _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Value.Should().Be("ABC Corporation");
        result.Value.Code.Value.Should().Be("CORP");
        result.Value.Status.Should().Be(OrganizationalUnitStatus.Active);
        result.Value.DomainEvents.Should().ContainSingle(e => e is OrganizationCreated);
    }

    [Fact]
    public void Create_Fails_WhenNameIsMissing()
    {
        var result = Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), null, "CORP", null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationNameRequired);
    }

    [Fact]
    public void Create_Fails_WhenCodeIsMissing()
    {
        var result = Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC", null, null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationCodeRequired);
    }

    [Fact]
    public void Rename_Succeeds_WhenActive()
    {
        var organization = CreateOrganization();

        var result = organization.Rename("New Name", _now);

        result.IsSuccess.Should().BeTrue();
        organization.Name.Value.Should().Be("New Name");
    }

    [Fact]
    public void Rename_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.Rename("New Name", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void Archive_Succeeds_WhenNoActiveChildrenExist()
    {
        var organization = CreateOrganization();

        var result = organization.Archive(_now);

        result.IsSuccess.Should().BeTrue();
        organization.Status.Should().Be(OrganizationalUnitStatus.Archived);
        organization.DomainEvents.Should().Contain(e => e is OrganizationArchived);
    }

    [Fact]
    public void Archive_Fails_WhenAnActiveBusinessUnitExistsBeneathIt()
    {
        var organization = CreateOrganization();
        organization.AddBusinessUnit("Corporate", _now);

        var result = organization.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.CannotArchiveWithActiveChildren);
    }

    [Fact]
    public void Archive_Succeeds_WhenEveryBusinessUnitIsAlreadyArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        organization.ArchiveBusinessUnit(businessUnitId, _now);

        var result = organization.Archive(_now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Archive_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void Restore_Succeeds_AfterArchive()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.Restore(_now);

        result.IsSuccess.Should().BeTrue();
        organization.Status.Should().Be(OrganizationalUnitStatus.Active);
    }

    [Fact]
    public void Restore_Fails_WhenNotArchived()
    {
        var organization = CreateOrganization();

        var result = organization.Restore(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NotArchived);
    }

    private static Hris.Modules.Organization.Domain.Organization CreateOrganization() =>
        Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, _now).Value;
}
