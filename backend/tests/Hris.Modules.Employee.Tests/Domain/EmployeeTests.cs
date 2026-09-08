using FluentAssertions;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Domain;

public sealed class EmployeeTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Hris.Modules.Employee.Domain.Employee.Create(
            new EmployeeId(Guid.NewGuid()), _tenantId, "EMP-000001", "Juan", "Santos", "Dela Cruz", null, null, null,
            new DateOnly(1990, 1, 1), "Manila", Gender.Male, CivilStatus.Single, "Filipino", "Philippine", TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStage.Should().Be(EmployeeLifecycleStage.Hired);
        result.Value.Name.FirstName.Should().Be("Juan");
    }

    [Fact]
    public void Create_WithoutFirstName_Fails()
    {
        var result = Hris.Modules.Employee.Domain.Employee.Create(
            new EmployeeId(Guid.NewGuid()), _tenantId, "EMP-000002", null, null, "Dela Cruz", null, null, null,
            new DateOnly(1990, 1, 1), null, Gender.Male, CivilStatus.Single, null, null, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FirstNameRequired);
    }

    [Fact]
    public void Create_WithFutureDateOfBirth_Fails()
    {
        var result = Hris.Modules.Employee.Domain.Employee.Create(
            new EmployeeId(Guid.NewGuid()), _tenantId, "EMP-000003", "Juan", null, "Dela Cruz", null, null, null,
            DateOnly.FromDateTime(TestEmployee.NowUtc.UtcDateTime.AddYears(1)), null, Gender.Male, CivilStatus.Single, null,
            null, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.DateOfBirthInFuture);
    }

    [Fact]
    public void StartOnboarding_FromHired_Succeeds()
    {
        var employee = TestEmployee.Create(_tenantId);

        var result = employee.StartOnboarding(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Onboarding);
    }

    [Fact]
    public void StartOnboarding_WhenNotHired_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.StartOnboarding(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotHired);
    }

    [Fact]
    public void Activate_FromOnboarding_Succeeds()
    {
        var employee = TestEmployee.Create(_tenantId);
        employee.StartOnboarding(TestEmployee.NowUtc);

        var result = employee.Activate(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active);
    }

    [Fact]
    public void Activate_WhenNotOnboarding_Fails()
    {
        var employee = TestEmployee.Create(_tenantId);

        var result = employee.Activate(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotOnboarding);
    }

    [Fact]
    public void StartOffboarding_FromActive_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.StartOffboarding(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Offboarding);
    }

    [Fact]
    public void StartOffboarding_WhenNotActive_Fails()
    {
        var employee = TestEmployee.Create(_tenantId);

        var result = employee.StartOffboarding(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotActive);
    }

    [Fact]
    public void Separate_FromOffboarding_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.StartOffboarding(TestEmployee.NowUtc);

        var result = employee.Separate(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Separated);
    }

    [Fact]
    public void Separate_WhenNotOffboarding_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.Separate(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotOffboarding);
    }

    [Fact]
    public void Retire_FromSeparated_Succeeds()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);

        var result = employee.Retire(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Retired);
    }

    [Fact]
    public void Retire_WhenNotSeparated_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.Retire(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotSeparated);
    }

    [Fact]
    public void Rehire_FromSeparated_ReturnsToActive()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);

        var result = employee.Rehire(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active);
    }

    [Fact]
    public void Rehire_WhenNotSeparated_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.Rehire(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotSeparated);
    }

    [Theory]
    [InlineData(EmployeeLifecycleStage.Hired)]
    [InlineData(EmployeeLifecycleStage.Active)]
    [InlineData(EmployeeLifecycleStage.Separated)]
    public void RecordDeceased_FromAnyNonDeceasedStage_Succeeds(EmployeeLifecycleStage startingStage)
    {
        var employee = startingStage switch
        {
            EmployeeLifecycleStage.Active => TestEmployee.CreateActive(_tenantId),
            EmployeeLifecycleStage.Separated => TestEmployee.CreateSeparated(_tenantId),
            _ => TestEmployee.Create(_tenantId),
        };

        var result = employee.RecordDeceased(TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Deceased);
    }

    [Fact]
    public void RecordDeceased_WhenAlreadyDeceased_Fails()
    {
        var employee = TestEmployee.Create(_tenantId);
        employee.RecordDeceased(TestEmployee.NowUtc);

        var result = employee.RecordDeceased(TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeAlreadyDeceased);
    }

    [Fact]
    public void UpdatePersonalInformation_WhenActive_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdatePersonalInformation(
            "Juana", null, "Dela Cruz", null, null, "Juju", new DateOnly(1990, 1, 1), "Cebu", Gender.Female,
            CivilStatus.Married, "Filipino", "Philippine", TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.Name.FirstName.Should().Be("Juana");
        employee.CivilStatus.Should().Be(CivilStatus.Married);
    }

    [Fact]
    public void UpdatePersonalInformation_WhenSeparated_Fails()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);

        var result = employee.UpdatePersonalInformation(
            "Juana", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null, Gender.Female,
            CivilStatus.Married, null, null, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeRecordIsReadOnly);
    }

    [Fact]
    public void UpdatePhoto_WhenActive_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        var reference = Guid.NewGuid();

        var result = employee.UpdatePhoto(reference, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.PhotographReference.Should().Be(reference);
    }

    [Fact]
    public void UpdateContactInformation_WithValidData_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateContactInformation(
            "juan@example.com", "juan@company.com", "+63 917 123 4567", null, "123 Main St", null, "Manila", "Metro Manila",
            "1000", "Philippines", null, null, null, null, null, null, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.PersonalEmail!.Value.Should().Be("juan@example.com");
        employee.HomeAddress!.City.Should().Be("Manila");
    }

    [Fact]
    public void UpdateContactInformation_WithInvalidEmail_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateContactInformation(
            "not-an-email", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateGovernmentInformation_WithValidData_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateGovernmentInformation("123-456-789", "12-3456789-0", null, null, null, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.Tin!.Value.Should().Be("123-456-789");
        employee.Sss!.Value.Should().Be("12-3456789-0");
    }

    [Fact]
    public void UpdateGovernmentInformation_WithInvalidTin_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateGovernmentInformation("not-a-tin!", null, null, null, null, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.TinInvalidFormat);
    }

    [Fact]
    public void UpdateBankingInformation_WithValidData_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateBankingInformation("BDO", "Makati", "Juan Dela Cruz", "1234567890", null, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.Banking!.BankName.Should().Be("BDO");
    }

    [Fact]
    public void UpdateBankingInformation_WithoutAnyValue_ClearsBanking()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.UpdateBankingInformation("BDO", null, null, "1234567890", null, TestEmployee.NowUtc);

        var result = employee.UpdateBankingInformation(null, null, null, null, null, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.Banking.Should().BeNull();
    }

    [Fact]
    public void AddEmergencyContact_WithValidData_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts.Should().ContainSingle();
        employee.EmergencyContacts[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void AddEmergencyContact_SecondPrimary_DemotesFirst()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);

        var result = employee.AddEmergencyContact("Pedro Santos", "Parent", "+63 917 333 4444", null, true, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts.Should().HaveCount(2);
        employee.EmergencyContacts.Single(c => c.Name == "Maria Santos").IsPrimary.Should().BeFalse();
        employee.EmergencyContacts.Single(c => c.Name == "Pedro Santos").IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void AddEmergencyContact_WithoutName_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.AddEmergencyContact(null, "Spouse", "+63 917 111 2222", null, false, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmergencyContactNameRequired);
    }

    [Fact]
    public void UpdateEmergencyContact_WithExistingId_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);
        var contactId = employee.EmergencyContacts[0].Id.Value;

        var result = employee.UpdateEmergencyContact(
            contactId, "Maria Reyes", "Spouse", "+63 917 999 8888", "maria@example.com", TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts[0].Name.Should().Be("Maria Reyes");
    }

    [Fact]
    public void UpdateEmergencyContact_WithUnknownId_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateEmergencyContact(Guid.NewGuid(), "Maria Reyes", "Spouse", "+63 917 999 8888", null, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmergencyContactNotFound);
    }

    [Fact]
    public void RemoveEmergencyContact_WithExistingId_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);
        var contactId = employee.EmergencyContacts[0].Id.Value;

        var result = employee.RemoveEmergencyContact(contactId, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts.Should().BeEmpty();
    }

    [Fact]
    public void RemoveEmergencyContact_WithUnknownId_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.RemoveEmergencyContact(Guid.NewGuid(), TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmergencyContactNotFound);
    }

    [Fact]
    public void AddFamilyMember_WithValidData_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, new DateOnly(1992, 3, 4), false, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.FamilyMembers.Should().ContainSingle();
    }

    [Fact]
    public void AddFamilyMember_WithoutName_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.AddFamilyMember(null, FamilyRelationship.Spouse, null, false, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FamilyMemberNameRequired);
    }

    [Fact]
    public void UpdateFamilyMember_WithExistingId_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, null, false, TestEmployee.NowUtc);
        var memberId = employee.FamilyMembers[0].Id.Value;

        var result = employee.UpdateFamilyMember(memberId, "Maria Reyes", FamilyRelationship.Spouse, null, true, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.FamilyMembers[0].IsDependent.Should().BeTrue();
    }

    [Fact]
    public void UpdateFamilyMember_WithUnknownId_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.UpdateFamilyMember(Guid.NewGuid(), "Maria Reyes", FamilyRelationship.Spouse, null, true, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FamilyMemberNotFound);
    }

    [Fact]
    public void RemoveFamilyMember_WithExistingId_Succeeds()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, null, false, TestEmployee.NowUtc);
        var memberId = employee.FamilyMembers[0].Id.Value;

        var result = employee.RemoveFamilyMember(memberId, TestEmployee.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employee.FamilyMembers.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFamilyMember_WithUnknownId_Fails()
    {
        var employee = TestEmployee.CreateActive(_tenantId);

        var result = employee.RemoveFamilyMember(Guid.NewGuid(), TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FamilyMemberNotFound);
    }

    [Fact]
    public void AddEmergencyContact_WhenSeparated_Fails()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);

        var result = employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeRecordIsReadOnly);
    }
}
