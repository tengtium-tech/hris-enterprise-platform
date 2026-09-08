using FluentAssertions;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Domain;

/// <summary>
/// Constructs every Domain Event record directly and asserts every property, the
/// identical technique <c>EmploymentDomainEventsTests</c> already establishes to
/// close the coverage gap `record` types with unread auto-generated getters leave
/// behind.
/// </summary>
public sealed class EmployeeDomainEventsTests
{
    private static readonly Guid _eventId = Guid.NewGuid();
    private static readonly DateTimeOffset _now = TestEmployee.NowUtc;
    private static readonly EmployeeId _employeeId = new(Guid.NewGuid());

    [Fact]
    public void EmployeeCreated_CarriesEveryProperty()
    {
        var tenantId = Guid.NewGuid();
        var evt = new EmployeeCreated(_eventId, _now, _employeeId, tenantId, "EMP-000001", "Juan", "Dela Cruz");

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.TenantId.Should().Be(tenantId);
        evt.EmployeeNumber.Should().Be("EMP-000001");
        evt.FirstName.Should().Be("Juan");
        evt.LastName.Should().Be("Dela Cruz");
    }

    [Fact]
    public void EmployeeOnboardingStarted_CarriesEveryProperty()
    {
        var evt = new EmployeeOnboardingStarted(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeActivated_CarriesEveryProperty()
    {
        var evt = new EmployeeActivated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeOffboardingStarted_CarriesEveryProperty()
    {
        var evt = new EmployeeOffboardingStarted(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeSeparated_CarriesEveryProperty()
    {
        var evt = new EmployeeSeparated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeRetired_CarriesEveryProperty()
    {
        var evt = new EmployeeRetired(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeDeceased_CarriesEveryProperty()
    {
        var evt = new EmployeeDeceased(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeePersonalInformationUpdated_CarriesEveryProperty()
    {
        var evt = new EmployeePersonalInformationUpdated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeContactInformationUpdated_CarriesEveryProperty()
    {
        var evt = new EmployeeContactInformationUpdated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeGovernmentInformationUpdated_CarriesEveryProperty()
    {
        var evt = new EmployeeGovernmentInformationUpdated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmployeeBankingInformationUpdated_CarriesEveryProperty()
    {
        var evt = new EmployeeBankingInformationUpdated(_eventId, _now, _employeeId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmergencyContactAdded_CarriesEveryProperty()
    {
        var contactId = new EmergencyContactId(Guid.NewGuid());
        var evt = new EmergencyContactAdded(_eventId, _now, _employeeId, contactId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.EmergencyContactId.Should().Be(contactId);
    }

    [Fact]
    public void EmergencyContactUpdated_CarriesEveryProperty()
    {
        var contactId = new EmergencyContactId(Guid.NewGuid());
        var evt = new EmergencyContactUpdated(_eventId, _now, _employeeId, contactId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.EmergencyContactId.Should().Be(contactId);
    }

    [Fact]
    public void EmergencyContactRemoved_CarriesEveryProperty()
    {
        var contactId = new EmergencyContactId(Guid.NewGuid());
        var evt = new EmergencyContactRemoved(_eventId, _now, _employeeId, contactId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.EmergencyContactId.Should().Be(contactId);
    }

    [Fact]
    public void FamilyMemberAdded_CarriesEveryProperty()
    {
        var memberId = new FamilyMemberId(Guid.NewGuid());
        var evt = new FamilyMemberAdded(_eventId, _now, _employeeId, memberId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.FamilyMemberId.Should().Be(memberId);
    }

    [Fact]
    public void FamilyMemberUpdated_CarriesEveryProperty()
    {
        var memberId = new FamilyMemberId(Guid.NewGuid());
        var evt = new FamilyMemberUpdated(_eventId, _now, _employeeId, memberId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.FamilyMemberId.Should().Be(memberId);
    }

    [Fact]
    public void FamilyMemberRemoved_CarriesEveryProperty()
    {
        var memberId = new FamilyMemberId(Guid.NewGuid());
        var evt = new FamilyMemberRemoved(_eventId, _now, _employeeId, memberId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.EmployeeId.Should().Be(_employeeId);
        evt.FamilyMemberId.Should().Be(memberId);
    }
}
