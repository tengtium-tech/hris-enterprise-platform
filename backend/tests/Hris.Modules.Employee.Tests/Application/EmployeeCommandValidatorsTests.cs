using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Application.Validators;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class EmployeeCommandValidatorsTests
{
    [Fact]
    public void RegisterEmployeeCommandValidator_WithoutFirstName_Fails()
    {
        var result = new RegisterEmployeeCommandValidator().Validate(new RegisterEmployeeCommand(
            Guid.NewGuid(), "EMP-000001", null, null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
            Gender.Male, CivilStatus.Single, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterEmployeeCommandValidator_Valid_Passes()
    {
        var result = new RegisterEmployeeCommandValidator().Validate(new RegisterEmployeeCommand(
            Guid.NewGuid(), "EMP-000001", "Juan", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
            Gender.Male, CivilStatus.Single, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateEmployeePersonalInformationCommandValidator_WithoutLastName_Fails()
    {
        var result = new UpdateEmployeePersonalInformationCommandValidator().Validate(new UpdateEmployeePersonalInformationCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Juan", null, null, null, null, null, new DateOnly(1990, 1, 1), null,
            Gender.Male, CivilStatus.Single, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateEmployeePhotoCommandValidator_WithoutEmployeeId_Fails()
    {
        var result = new UpdateEmployeePhotoCommandValidator().Validate(new UpdateEmployeePhotoCommand(Guid.Empty, Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartEmployeeOnboardingCommandValidator_WithoutTenantId_Fails()
    {
        var result = new StartEmployeeOnboardingCommandValidator().Validate(
            new StartEmployeeOnboardingCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateEmployeeCommandValidator_Valid_Passes()
    {
        var result = new ActivateEmployeeCommandValidator().Validate(new ActivateEmployeeCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void StartEmployeeOffboardingCommandValidator_Valid_Passes()
    {
        var result = new StartEmployeeOffboardingCommandValidator().Validate(
            new StartEmployeeOffboardingCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SeparateEmployeeCommandValidator_Valid_Passes()
    {
        var result = new SeparateEmployeeCommandValidator().Validate(new SeparateEmployeeCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RetireEmployeeCommandValidator_Valid_Passes()
    {
        var result = new RetireEmployeeCommandValidator().Validate(new RetireEmployeeCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RehireEmployeeCommandValidator_Valid_Passes()
    {
        var result = new RehireEmployeeCommandValidator().Validate(new RehireEmployeeCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordEmployeeDeceasedCommandValidator_Valid_Passes()
    {
        var result = new RecordEmployeeDeceasedCommandValidator().Validate(
            new RecordEmployeeDeceasedCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateEmployeeContactInformationCommandValidator_Valid_Passes()
    {
        var result = new UpdateEmployeeContactInformationCommandValidator().Validate(new UpdateEmployeeContactInformationCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateEmployeeGovernmentInformationCommandValidator_WithoutEmployeeId_Fails()
    {
        var result = new UpdateEmployeeGovernmentInformationCommandValidator().Validate(
            new UpdateEmployeeGovernmentInformationCommand(Guid.Empty, Guid.NewGuid(), null, null, null, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateEmployeeBankInformationCommandValidator_WithoutTenantId_Fails()
    {
        var result = new UpdateEmployeeBankInformationCommandValidator().Validate(
            new UpdateEmployeeBankInformationCommand(Guid.NewGuid(), Guid.Empty, null, null, null, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddEmergencyContactCommandValidator_WithoutName_Fails()
    {
        var result = new AddEmergencyContactCommandValidator().Validate(new AddEmergencyContactCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, "Spouse", "+63 917 111 2222", null, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateEmergencyContactCommandValidator_WithoutPhone_Fails()
    {
        var result = new UpdateEmergencyContactCommandValidator().Validate(new UpdateEmergencyContactCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Maria Santos", "Spouse", null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveEmergencyContactCommandValidator_WithoutEmergencyContactId_Fails()
    {
        var result = new RemoveEmergencyContactCommandValidator().Validate(
            new RemoveEmergencyContactCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddFamilyMemberCommandValidator_WithoutName_Fails()
    {
        var result = new AddFamilyMemberCommandValidator().Validate(new AddFamilyMemberCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, FamilyRelationship.Spouse, null, false));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateFamilyMemberCommandValidator_WithoutFamilyMemberId_Fails()
    {
        var result = new UpdateFamilyMemberCommandValidator().Validate(new UpdateFamilyMemberCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Maria Dela Cruz", FamilyRelationship.Spouse, null, false));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveFamilyMemberCommandValidator_Valid_Passes()
    {
        var result = new RemoveFamilyMemberCommandValidator().Validate(
            new RemoveFamilyMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
