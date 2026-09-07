using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

public sealed class WorkLocationTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Succeeds_WithValidData()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        var result = WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), null, address,
            timeZone, null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Address.City.Should().Be("Makati");
        result.Value.DomainEvents.Should().ContainSingle(e => e is WorkLocationCreated);
    }

    [Fact]
    public void Create_Fails_WhenTimeZoneIsNotARecognizedIanaIdentifier()
    {
        var timeZoneResult = WorkLocationTimeZone.Create("Not/AZone");

        timeZoneResult.IsFailure.Should().BeTrue();
        timeZoneResult.Error.Should().Be(OrganizationErrors.TimeZoneInvalid);
    }

    [Fact]
    public void Archive_ThenRestore_ReturnsToActive()
    {
        var workLocation = CreateWorkLocation();
        workLocation.Archive(_now);

        var result = workLocation.Restore(_now);

        result.IsSuccess.Should().BeTrue();
        workLocation.Status.Should().Be(OrganizationalUnitStatus.Active);
    }

    [Fact]
    public void Restore_Fails_WhenNotArchived()
    {
        var workLocation = CreateWorkLocation();

        var result = workLocation.Restore(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NotArchived);
    }

    private static WorkLocation CreateWorkLocation()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        return WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), null, address,
            timeZone, null, _now).Value;
    }
}
