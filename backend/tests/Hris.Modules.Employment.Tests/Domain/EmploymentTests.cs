using FluentAssertions;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Domain;

public sealed class EmploymentTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000001", "Regular", "Rank-and-File", true,
            null, null, true, false, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStage.Should().Be(EmploymentLifecycleStage.Draft);
        result.Value.OperationalStatus.Should().Be(OperationalStatus.Active);
        result.Value.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutNumber_Fails()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), null, "Regular", "Rank-and-File", true, null,
            null, true, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNumberRequired);
    }

    [Fact]
    public void Create_SecondaryWithoutConcurrentEmploymentEnabled_Fails()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000002", "Consultant", "Rank-and-File",
            false, Guid.NewGuid(), null, false, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ConcurrentEmploymentNotEnabled);
    }

    [Fact]
    public void Create_PrimaryWithExistingPrimaryAndConcurrentDisabled_Fails()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000003", "Regular", "Rank-and-File", true,
            null, null, false, true, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.DuplicatePrimaryEmployment);
    }

    [Fact]
    public void Activate_FromDraftWithValidContractAndAssignment_Succeeds()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Activate(true, true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Active);
    }

    [Fact]
    public void Activate_WithoutValidContract_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Activate(false, true, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentActivationRequiresContract);
    }

    [Fact]
    public void Activate_WithoutValidAssignment_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Activate(true, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentActivationRequiresAssignment);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.Activate(true, true, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotDraft);
    }

    [Fact]
    public void ChangeEmploymentType_WhenActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ChangeEmploymentType("Contractual", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.EmploymentType.Value.Should().Be("Contractual");
    }

    [Fact]
    public void ChangeEmploymentType_WhenSeparated_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.Separate(SeparationType.Resigned, null, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime),
            DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        var result = employment.ChangeEmploymentType("Regular", TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentSeparatedCannotBeModified);
    }

    [Fact]
    public void ChangeEmploymentCategory_WhenActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ChangeEmploymentCategory("Supervisory", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.Category.Value.Should().Be("Supervisory");
    }

    [Fact]
    public void RecordCompensation_FirstRecord_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.RecordCompensation(
            50000m, "PHP", CompensationBasis.Monthly, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime),
            CompensationChangeSource.Hire, null, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.CompensationRecords.Should().HaveCount(1);
        employment.CompensationRecords[0].EffectiveEndDate.Should().BeNull();
    }

    [Fact]
    public void RecordCompensation_SecondRecord_ClosesFirst()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var startDate = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.RecordCompensation(50000m, "PHP", CompensationBasis.Monthly, startDate, CompensationChangeSource.Hire, null, TestEmployment.NowUtc);

        var result = employment.RecordCompensation(
            55000m, "PHP", CompensationBasis.Monthly, startDate.AddYears(1), CompensationChangeSource.CompensationChange,
            "CC-001", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.CompensationRecords.Should().HaveCount(2);
        employment.CompensationRecords[0].EffectiveEndDate.Should().Be(startDate.AddYears(1));
        employment.CompensationRecords[1].EffectiveEndDate.Should().BeNull();
    }

    [Fact]
    public void RecordCompensation_WithNegativeAmount_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.RecordCompensation(
            -1m, "PHP", CompensationBasis.Monthly, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime),
            CompensationChangeSource.Hire, null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CompensationAmountNegative);
    }

    [Fact]
    public void StartProbation_WhenActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords.Should().HaveCount(1);
        employment.ProbationRecords[0].Outcome.Should().Be(ProbationOutcome.Pending);
    }

    [Fact]
    public void StartProbation_WhenAlreadyStarted_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        var result = employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationAlreadyStarted);
    }

    [Fact]
    public void StartProbation_WhenDraft_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotActive);
    }

    [Fact]
    public void ExtendProbation_WhenInProgress_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var startDate = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.StartProbation(startDate, 180, TestEmployment.NowUtc);
        var originalEvaluationDate = employment.ProbationRecords[0].ExpectedEvaluationDate;

        var result = employment.ExtendProbation(30, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords[0].ExpectedEvaluationDate.Should().Be(originalEvaluationDate.AddDays(30));
        employment.ProbationRecords[0].ExtensionCount.Should().Be(1);
    }

    [Fact]
    public void ExtendProbation_WithoutProbation_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ExtendProbation(30, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationNotInProgress);
    }

    [Fact]
    public void ConfirmEmployment_WithProbationInProgress_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        var result = employment.ConfirmEmployment("Regular", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords[0].Outcome.Should().Be(ProbationOutcome.Confirmed);
        employment.EmploymentType.Value.Should().Be("Regular");
    }

    [Fact]
    public void ConfirmEmployment_WithoutProbation_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ConfirmEmployment(null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationNotInProgress);
    }

    [Fact]
    public void FailProbation_WithProbationInProgress_SeparatesEmployment()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.FailProbation(date, date, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords[0].Outcome.Should().Be(ProbationOutcome.Failed);
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Separated);
        employment.SeparationRecord.Should().NotBeNull();
        employment.SeparationRecord!.SeparationType.Should().Be(SeparationType.Terminated);
    }

    [Fact]
    public void Suspend_WhenActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.Suspend("Under investigation", DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Suspended);
        employment.StatusChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Suspend("Reason", date, TestEmployment.NowUtc);

        var result = employment.Suspend("Reason", date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.OperationalStatusUnchanged);
    }

    [Fact]
    public void Reinstate_FromSuspended_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Suspend("Reason", date, TestEmployment.NowUtc);

        var result = employment.Reinstate(date, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Active);
    }

    [Fact]
    public void Reinstate_WhenAlreadyActive_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.Reinstate(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.OperationalStatusUnchanged);
    }

    [Fact]
    public void Second_WhenActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.Second(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Seconded);
    }

    [Fact]
    public void Separate_FromActive_Succeeds()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.Separate(SeparationType.Resigned, "Better offer", date, date, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Separated);
        employment.SeparationRecord!.TerminationReason!.Value.Should().Be("Better offer");
    }

    [Fact]
    public void Separate_FromDraft_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.Separate(SeparationType.Resigned, null, date, date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotActive);
    }

    [Fact]
    public void Separate_WhenAlreadySeparated_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Separate(SeparationType.Resigned, null, date, date, TestEmployment.NowUtc);

        var result = employment.Separate(SeparationType.Resigned, null, date, date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentAlreadySeparated);
    }

    [Fact]
    public void Separate_SecondaryEmployment_RaisesConcurrentEmploymentEnded()
    {
        var employeeId = Guid.NewGuid();
        var employment = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, employeeId, "EMP-000009", "Consultant", "Rank-and-File", false,
            Guid.NewGuid(), null, true, false, TestEmployment.NowUtc).Value;
        employment.Activate(true, true, TestEmployment.NowUtc);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.Separate(SeparationType.EndOfContract, null, date, date, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.DomainEvents.Should().Contain(e => e is ConcurrentEmploymentEnded);
    }

    [Fact]
    public void MarkPrimary_WhenSecondary_Succeeds()
    {
        var employment = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000010", "Consultant", "Rank-and-File",
            false, Guid.NewGuid(), null, true, false, TestEmployment.NowUtc).Value;

        var result = employment.MarkPrimary(Guid.NewGuid(), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void MarkPrimary_WhenAlreadyPrimary_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.MarkPrimary(Guid.NewGuid(), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentAlreadyPrimary);
    }

    [Fact]
    public void MarkSecondary_SetsIsPrimaryFalse()
    {
        var employment = TestEmployment.Create(_tenantId);

        employment.MarkSecondary();

        employment.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Create_WithInvalidEmploymentType_Fails()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000020", new string('A', 101),
            "Rank-and-File", true, null, null, true, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentTypeTooLong);
    }

    [Fact]
    public void Create_WithInvalidCategory_Fails()
    {
        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000021", "Regular", null, true, null,
            null, true, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentCategoryRequired);
    }

    [Fact]
    public void Create_Rehire_RaisesEmployeeRehired()
    {
        var priorEmploymentId = Guid.NewGuid();

        var result = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "EMP-000022", "Regular", "Rank-and-File", true,
            null, priorEmploymentId, true, false, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.PriorEmploymentId.Should().Be(priorEmploymentId);
        result.Value.DomainEvents.Should().Contain(e => e is EmployeeRehired);
    }

    [Fact]
    public void ChangeEmploymentType_WithInvalidType_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ChangeEmploymentType(new string('A', 101), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentTypeTooLong);
    }

    [Fact]
    public void ChangeEmploymentCategory_WhenSeparated_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Separate(SeparationType.Resigned, null, date, date, TestEmployment.NowUtc);

        var result = employment.ChangeEmploymentCategory("Managerial", TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentSeparatedCannotBeModified);
    }

    [Fact]
    public void ChangeEmploymentCategory_WithInvalidCategory_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.ChangeEmploymentCategory(new string('A', 101), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentCategoryTooLong);
    }

    [Fact]
    public void RecordCompensation_WhenSeparated_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Separate(SeparationType.Resigned, null, date, date, TestEmployment.NowUtc);

        var result = employment.RecordCompensation(
            50000m, "PHP", CompensationBasis.Monthly, date, CompensationChangeSource.Hire, null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentSeparatedCannotBeModified);
    }

    [Fact]
    public void StartProbation_WithInvalidDuration_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 0, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationDurationMustBePositive);
    }

    [Fact]
    public void ExtendProbation_WithInvalidDuration_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        var result = employment.ExtendProbation(0, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationDurationMustBePositive);
    }

    [Fact]
    public void ConfirmEmployment_WithInvalidNewEmploymentType_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);

        var result = employment.ConfirmEmployment(new string('A', 101), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentTypeTooLong);
    }

    [Fact]
    public void FailProbation_WithoutProbation_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.FailProbation(date, date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationNotInProgress);
    }

    [Fact]
    public void Suspend_WhenDraft_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Suspend("Reason", DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotActive);
    }

    [Fact]
    public void Suspend_WithNullReason_NormalizesToEmpty()
    {
        var employment = TestEmployment.CreateActive(_tenantId);

        var result = employment.Suspend(null, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        employment.StatusChanges[0].Reason.Should().BeEmpty();
    }

    [Fact]
    public void Reinstate_WhenDraft_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Reinstate(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotActive);
    }

    [Fact]
    public void Second_WhenDraft_Fails()
    {
        var employment = TestEmployment.Create(_tenantId);

        var result = employment.Second(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotActive);
    }

    [Fact]
    public void Second_WhenAlreadySeconded_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Second(date, TestEmployment.NowUtc);

        var result = employment.Second(date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.OperationalStatusUnchanged);
    }

    [Fact]
    public void Separate_WithInvalidTerminationReason_Fails()
    {
        var employment = TestEmployment.CreateActive(_tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

        var result = employment.Separate(SeparationType.Terminated, new string('A', 501), date, date, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.TerminationReasonTooLong);
    }
}
