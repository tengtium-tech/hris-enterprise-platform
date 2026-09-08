using FluentAssertions;
using Hris.Modules.Employment.Application.Commands;
using Hris.Modules.Employment.Application.Validators;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class EmploymentCommandValidatorsTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

    [Fact]
    public void CreateEmploymentCommandValidator_Valid_Passes()
    {
        var result = new CreateEmploymentCommandValidator().Validate(
            new CreateEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), "EMP-000001", "Regular", "Rank-and-File", true, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEmploymentCommandValidator_MissingPrimaryEmploymentIdWhenSecondary_Fails()
    {
        var result = new CreateEmploymentCommandValidator().Validate(
            new CreateEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), "EMP-000001", "Regular", "Rank-and-File", false, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateEmploymentCommandValidator_Valid_Passes()
    {
        var result = new ActivateEmploymentCommandValidator().Validate(new ActivateEmploymentCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ActivateEmploymentCommandValidator_MissingEmploymentId_Fails()
    {
        var result = new ActivateEmploymentCommandValidator().Validate(new ActivateEmploymentCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangeEmploymentTypeCommandValidator_Valid_Passes()
    {
        var result = new ChangeEmploymentTypeCommandValidator().Validate(
            new ChangeEmploymentTypeCommand(Guid.NewGuid(), Guid.NewGuid(), "Regular"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeEmploymentTypeCommandValidator_MissingNewType_Fails()
    {
        var result = new ChangeEmploymentTypeCommandValidator().Validate(
            new ChangeEmploymentTypeCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangeEmploymentCategoryCommandValidator_Valid_Passes()
    {
        var result = new ChangeEmploymentCategoryCommandValidator().Validate(
            new ChangeEmploymentCategoryCommand(Guid.NewGuid(), Guid.NewGuid(), "Managerial"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeEmploymentCategoryCommandValidator_MissingNewCategory_Fails()
    {
        var result = new ChangeEmploymentCategoryCommandValidator().Validate(
            new ChangeEmploymentCategoryCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangePrimaryEmploymentCommandValidator_Valid_Passes()
    {
        var result = new ChangePrimaryEmploymentCommandValidator().Validate(
            new ChangePrimaryEmploymentCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePrimaryEmploymentCommandValidator_MissingEmploymentId_Fails()
    {
        var result = new ChangePrimaryEmploymentCommandValidator().Validate(
            new ChangePrimaryEmploymentCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SuspendEmploymentCommandValidator_Valid_Passes()
    {
        var result = new SuspendEmploymentCommandValidator().Validate(
            new SuspendEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), "Reason", Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SuspendEmploymentCommandValidator_MissingTenantId_Fails()
    {
        var result = new SuspendEmploymentCommandValidator().Validate(
            new SuspendEmploymentCommand(Guid.NewGuid(), Guid.Empty, "Reason", Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReinstateEmploymentCommandValidator_Valid_Passes()
    {
        var result = new ReinstateEmploymentCommandValidator().Validate(
            new ReinstateEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReinstateEmploymentCommandValidator_MissingEmploymentId_Fails()
    {
        var result = new ReinstateEmploymentCommandValidator().Validate(
            new ReinstateEmploymentCommand(Guid.Empty, Guid.NewGuid(), Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SecondEmploymentCommandValidator_Valid_Passes()
    {
        var result = new SecondEmploymentCommandValidator().Validate(
            new SecondEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SecondEmploymentCommandValidator_MissingEmploymentId_Fails()
    {
        var result = new SecondEmploymentCommandValidator().Validate(
            new SecondEmploymentCommand(Guid.Empty, Guid.NewGuid(), Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SeparateEmploymentCommandValidator_Valid_Passes()
    {
        var result = new SeparateEmploymentCommandValidator().Validate(
            new SeparateEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), SeparationType.Resigned, null, Today, Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SeparateEmploymentCommandValidator_LastWorkingDateAfterEffectiveDate_Fails()
    {
        var result = new SeparateEmploymentCommandValidator().Validate(
            new SeparateEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), SeparationType.Resigned, null, Today.AddDays(5), Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartProbationCommandValidator_Valid_Passes()
    {
        var result = new StartProbationCommandValidator().Validate(new StartProbationCommand(Guid.NewGuid(), Guid.NewGuid(), Today, 180));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void StartProbationCommandValidator_ZeroDuration_Fails()
    {
        var result = new StartProbationCommandValidator().Validate(new StartProbationCommand(Guid.NewGuid(), Guid.NewGuid(), Today, 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExtendProbationCommandValidator_Valid_Passes()
    {
        var result = new ExtendProbationCommandValidator().Validate(new ExtendProbationCommand(Guid.NewGuid(), Guid.NewGuid(), 30));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExtendProbationCommandValidator_ZeroDuration_Fails()
    {
        var result = new ExtendProbationCommandValidator().Validate(new ExtendProbationCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmEmploymentCommandValidator_Valid_Passes()
    {
        var result = new ConfirmEmploymentCommandValidator().Validate(
            new ConfirmEmploymentCommand(Guid.NewGuid(), Guid.NewGuid(), "Regular"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmploymentCommandValidator_MissingEmploymentId_Fails()
    {
        var result = new ConfirmEmploymentCommandValidator().Validate(
            new ConfirmEmploymentCommand(Guid.Empty, Guid.NewGuid(), "Regular"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailProbationCommandValidator_Valid_Passes()
    {
        var result = new FailProbationCommandValidator().Validate(new FailProbationCommand(Guid.NewGuid(), Guid.NewGuid(), Today, Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailProbationCommandValidator_LastWorkingDateAfterEffectiveDate_Fails()
    {
        var result = new FailProbationCommandValidator().Validate(
            new FailProbationCommand(Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(5), Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordEmploymentCompensationCommandValidator_Valid_Passes()
    {
        var result = new RecordEmploymentCompensationCommandValidator().Validate(
            new RecordEmploymentCompensationCommand(
                Guid.NewGuid(), Guid.NewGuid(), 50000m, "PHP", CompensationBasis.Monthly, Today, CompensationChangeSource.Hire, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordEmploymentCompensationCommandValidator_NegativeAmount_Fails()
    {
        var result = new RecordEmploymentCompensationCommandValidator().Validate(
            new RecordEmploymentCompensationCommand(
                Guid.NewGuid(), Guid.NewGuid(), -1m, "PHP", CompensationBasis.Monthly, Today, CompensationChangeSource.Hire, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new CreateEmploymentContractCommandValidator().Validate(
            new CreateEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid(), "Regular", Today, null, false, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEmploymentContractCommandValidator_MissingContractType_Fails()
    {
        var result = new CreateEmploymentContractCommandValidator().Validate(
            new CreateEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid(), null, Today, null, false, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApproveEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new ApproveEmploymentContractCommandValidator().Validate(
            new ApproveEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new ApproveEmploymentContractCommandValidator().Validate(
            new ApproveEmploymentContractCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new ActivateEmploymentContractCommandValidator().Validate(
            new ActivateEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ActivateEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new ActivateEmploymentContractCommandValidator().Validate(
            new ActivateEmploymentContractCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenewEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new RenewEmploymentContractCommandValidator().Validate(
            new RenewEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid(), Today, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RenewEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new RenewEmploymentContractCommandValidator().Validate(
            new RenewEmploymentContractCommand(Guid.Empty, Guid.NewGuid(), Today, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExtendEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new ExtendEmploymentContractCommandValidator().Validate(
            new ExtendEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid(), Today, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExtendEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new ExtendEmploymentContractCommandValidator().Validate(
            new ExtendEmploymentContractCommand(Guid.Empty, Guid.NewGuid(), Today, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SupersedeEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new SupersedeEmploymentContractCommandValidator().Validate(
            new SupersedeEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SupersedeEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new SupersedeEmploymentContractCommandValidator().Validate(
            new SupersedeEmploymentContractCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CloseEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new CloseEmploymentContractCommandValidator().Validate(
            new CloseEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CloseEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new CloseEmploymentContractCommandValidator().Validate(
            new CloseEmploymentContractCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelEmploymentContractCommandValidator_Valid_Passes()
    {
        var result = new CancelEmploymentContractCommandValidator().Validate(
            new CancelEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelEmploymentContractCommandValidator_MissingContractId_Fails()
    {
        var result = new CancelEmploymentContractCommandValidator().Validate(
            new CancelEmploymentContractCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignPositionCommandValidator_Valid_Passes()
    {
        var result = new AssignPositionCommandValidator().Validate(
            new AssignPositionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                null, Today, true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AssignPositionCommandValidator_MissingPositionId_Fails()
    {
        var result = new AssignPositionCommandValidator().Validate(
            new AssignPositionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, null, null, WorkArrangement.OnSite, null,
                Today, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TransferEmploymentCommandValidator_Valid_Passes()
    {
        var result = new TransferEmploymentCommandValidator().Validate(
            new TransferEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                Today, null, true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TransferEmploymentCommandValidator_MissingNewPositionId_Fails()
    {
        var result = new TransferEmploymentCommandValidator().Validate(
            new TransferEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, null, null, WorkArrangement.OnSite, Today,
                null, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PromoteEmploymentCommandValidator_Valid_Passes()
    {
        var result = new PromoteEmploymentCommandValidator().Validate(
            new PromoteEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                Today, null, true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PromoteEmploymentCommandValidator_MissingNewPositionId_Fails()
    {
        var result = new PromoteEmploymentCommandValidator().Validate(
            new PromoteEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, null, null, WorkArrangement.OnSite, Today,
                null, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DemoteEmploymentCommandValidator_Valid_Passes()
    {
        var result = new DemoteEmploymentCommandValidator().Validate(
            new DemoteEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                Today, null, true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DemoteEmploymentCommandValidator_MissingNewPositionId_Fails()
    {
        var result = new DemoteEmploymentCommandValidator().Validate(
            new DemoteEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, null, null, WorkArrangement.OnSite, Today,
                null, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChangeReportingManagerCommandValidator_Valid_Passes()
    {
        var result = new ChangeReportingManagerCommandValidator().Validate(
            new ChangeReportingManagerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeReportingManagerCommandValidator_MissingNewManagerId_Fails()
    {
        var result = new ChangeReportingManagerCommandValidator().Validate(
            new ChangeReportingManagerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Today));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndAssignmentCommandValidator_Valid_Passes()
    {
        var result = new EndAssignmentCommandValidator().Validate(new EndAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Today));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EndAssignmentCommandValidator_MissingAssignmentId_Fails()
    {
        var result = new EndAssignmentCommandValidator().Validate(new EndAssignmentCommand(Guid.Empty, Guid.NewGuid(), Today));

        result.IsValid.Should().BeFalse();
    }
}
