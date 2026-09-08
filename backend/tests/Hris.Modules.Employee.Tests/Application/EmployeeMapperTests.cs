using FluentAssertions;
using Hris.Modules.Employee.Application.Mapping;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

/// <summary>
/// Maps a fully-populated <see cref="Hris.Modules.Employee.Domain.Employee"/> --
/// every optional field non-null, every child collection non-empty -- and asserts
/// every DTO field, the identical technique <c>EmploymentMapperTests</c> already
/// establishes to close the coverage gap unread mapper branches leave behind.
/// </summary>
public sealed class EmployeeMapperTests
{
    [Fact]
    public void ToDto_MapsEveryField()
    {
        var tenantId = Guid.NewGuid();
        var employee = TestEmployee.CreateActive(tenantId);
        var photoRef = Guid.NewGuid();

        employee.UpdatePhoto(photoRef, TestEmployee.NowUtc);
        employee.UpdateContactInformation(
            "juan@example.com", "juan@company.com", "+63 917 123 4567", "+63 2 8888 1234", "123 Main St", "Unit 4",
            "Manila", "Metro Manila", "1000", "Philippines", "456 Other St", null, "Cebu", null, "6000", "Philippines",
            TestEmployee.NowUtc);
        employee.UpdateGovernmentInformation("123-456-789", "12-3456789-0", "12-345678901-2", "1234-5678-9012", null, TestEmployee.NowUtc);
        employee.UpdateBankingInformation("BDO", "Makati", "Juan Dela Cruz", "1234567890", "BNORPHMM", TestEmployee.NowUtc);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", "maria@example.com", true, TestEmployee.NowUtc);
        employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, new DateOnly(1992, 3, 4), true, TestEmployee.NowUtc);

        var dto = EmployeeMapper.ToDto(employee);

        dto.Id.Should().Be(employee.Id.Value);
        dto.TenantId.Should().Be(tenantId);
        dto.Number.Should().Be(employee.Number.Value);
        dto.FirstName.Should().Be("Juan");
        dto.LastName.Should().Be("Dela Cruz");
        dto.DateOfBirth.Should().Be(employee.DateOfBirth);
        dto.Gender.Should().Be(Gender.Male.ToString());
        dto.CivilStatus.Should().Be(CivilStatus.Single.ToString());
        dto.PhotographReference.Should().Be(photoRef);
        dto.SignatureReference.Should().BeNull();
        dto.PersonalEmail.Should().Be("juan@example.com");
        dto.CompanyEmail.Should().Be("juan@company.com");
        dto.MobileNumber.Should().Be("+63 917 123 4567");
        dto.TelephoneNumber.Should().Be("+63 2 8888 1234");
        dto.HomeAddress.Should().NotBeNull();
        dto.HomeAddress!.City.Should().Be("Manila");
        dto.MailingAddress.Should().NotBeNull();
        dto.MailingAddress!.City.Should().Be("Cebu");
        dto.Tin.Should().Be("123-456-789");
        dto.Sss.Should().Be("12-3456789-0");
        dto.PhilHealth.Should().Be("12-345678901-2");
        dto.PagIbig.Should().Be("1234-5678-9012");
        dto.Gsis.Should().BeNull();
        dto.Banking.Should().NotBeNull();
        dto.Banking!.BankName.Should().Be("BDO");
        dto.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active.ToString());
        dto.EmergencyContacts.Should().ContainSingle();
        dto.EmergencyContacts[0].Name.Should().Be("Maria Santos");
        dto.EmergencyContacts[0].Email.Should().Be("maria@example.com");
        dto.FamilyMembers.Should().ContainSingle();
        dto.FamilyMembers[0].Name.Should().Be("Maria Dela Cruz");
        dto.FamilyMembers[0].IsDependent.Should().BeTrue();
    }

    [Fact]
    public void ToSummaryDto_MapsEveryField()
    {
        var employee = TestEmployee.CreateActive(Guid.NewGuid());

        var dto = EmployeeMapper.ToSummaryDto(employee);

        dto.Id.Should().Be(employee.Id.Value);
        dto.Number.Should().Be(employee.Number.Value);
        dto.FirstName.Should().Be("Juan");
        dto.LastName.Should().Be("Dela Cruz");
        dto.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active.ToString());
    }

    [Fact]
    public void ToDto_History_MapsEveryField()
    {
        var employeeId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var history = EmployeeHistory.Record(
            new EmployeeHistoryId(Guid.NewGuid()), Guid.NewGuid(), employeeId, EmployeeHistoryCategory.LifecycleStage,
            "Hired", "Active", DateOnly.FromDateTime(TestEmployee.NowUtc.UtcDateTime), "Onboarding completed", changedBy,
            TestEmployee.NowUtc);

        var dto = EmployeeMapper.ToDto(history);

        dto.Id.Should().Be(history.Id.Value);
        dto.EmployeeId.Should().Be(employeeId);
        dto.Category.Should().Be(EmployeeHistoryCategory.LifecycleStage.ToString());
        dto.PreviousValue.Should().Be("Hired");
        dto.NewValue.Should().Be("Active");
        dto.BusinessReason.Should().Be("Onboarding completed");
        dto.ChangedBy.Should().Be(changedBy);
        dto.CreatedAtUtc.Should().Be(TestEmployee.NowUtc);
    }
}
