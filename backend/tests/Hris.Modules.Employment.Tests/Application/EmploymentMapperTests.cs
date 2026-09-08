using FluentAssertions;
using Hris.Modules.Employment.Application.Mapping;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

/// <summary>
/// Maps fully-populated Employment, EmploymentContract, and EmploymentAssignment
/// aggregates -- every child collection non-empty -- and asserts every resulting DTO
/// field, since aggregate-level Domain tests construct these but rarely read every
/// DTO field the way a real API response consumer would.
/// </summary>
public sealed class EmploymentMapperTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

    [Fact]
    public void ToDto_Employment_MapsEveryField()
    {
        var employment = TestEmployment.CreateActive(TenantId);
        employment.RecordCompensation(50000m, "PHP", CompensationBasis.Monthly, Today, CompensationChangeSource.Hire, "REF-1", TestEmployment.NowUtc);
        employment.StartProbation(Today, 180, TestEmployment.NowUtc);
        employment.Suspend("Investigation", Today, TestEmployment.NowUtc);
        employment.Reinstate(Today, TestEmployment.NowUtc);
        employment.Separate(SeparationType.Resigned, "New opportunity", Today, Today, TestEmployment.NowUtc);

        var dto = EmploymentMapper.ToDto(employment);

        dto.Id.Should().Be(employment.Id.Value);
        dto.TenantId.Should().Be(employment.TenantId);
        dto.EmployeeId.Should().Be(employment.EmployeeId);
        dto.Number.Should().Be("EMP-000001");
        dto.EmploymentType.Should().Be("Regular");
        dto.Category.Should().Be("Rank-and-File");
        dto.LifecycleStage.Should().Be(nameof(EmploymentLifecycleStage.Separated));
        dto.IsPrimary.Should().BeTrue();
        dto.PriorEmploymentId.Should().BeNull();
        dto.CreatedAtUtc.Should().Be(employment.CreatedAtUtc);

        dto.StatusChanges.Should().HaveCount(2);
        var statusChange = dto.StatusChanges[0];
        statusChange.Id.Should().NotBeEmpty();
        statusChange.PreviousStatus.Should().Be(nameof(OperationalStatus.Active));
        statusChange.NewStatus.Should().Be(nameof(OperationalStatus.Suspended));
        statusChange.EffectiveDate.Should().Be(Today);
        statusChange.Reason.Should().Be("Investigation");

        dto.ProbationRecords.Should().HaveCount(1);
        var probation = dto.ProbationRecords[0];
        probation.Id.Should().NotBeEmpty();
        probation.StartDate.Should().Be(Today);
        probation.DurationDays.Should().Be(180);
        probation.ExpectedEvaluationDate.Should().Be(Today.AddDays(180));
        probation.Outcome.Should().Be(nameof(ProbationOutcome.Pending));
        probation.ExtensionCount.Should().Be(0);

        dto.CompensationRecords.Should().HaveCount(1);
        var compensation = dto.CompensationRecords[0];
        compensation.Id.Should().NotBeEmpty();
        compensation.Amount.Should().Be(50000m);
        compensation.CurrencyCode.Should().Be("PHP");
        compensation.Basis.Should().Be(nameof(CompensationBasis.Monthly));
        compensation.EffectiveStartDate.Should().Be(Today);
        compensation.EffectiveEndDate.Should().BeNull();
        compensation.ChangeSource.Should().Be(nameof(CompensationChangeSource.Hire));
        compensation.ApprovalReference.Should().Be("REF-1");

        dto.SeparationRecord.Should().NotBeNull();
        dto.SeparationRecord!.Id.Should().NotBeEmpty();
        dto.SeparationRecord.SeparationType.Should().Be(nameof(SeparationType.Resigned));
        dto.SeparationRecord.TerminationReason.Should().Be("New opportunity");
        dto.SeparationRecord.LastWorkingDate.Should().Be(Today);
        dto.SeparationRecord.EffectiveSeparationDate.Should().Be(Today);
    }

    [Fact]
    public void ToSummaryDto_Employment_MapsEveryField()
    {
        var employment = TestEmployment.Create(TenantId);

        var dto = EmploymentMapper.ToSummaryDto(employment);

        dto.Id.Should().Be(employment.Id.Value);
        dto.EmployeeId.Should().Be(employment.EmployeeId);
        dto.Number.Should().Be("EMP-000001");
        dto.EmploymentType.Should().Be("Regular");
        dto.Category.Should().Be("Rank-and-File");
        dto.LifecycleStage.Should().Be(nameof(EmploymentLifecycleStage.Draft));
        dto.OperationalStatus.Should().Be(nameof(OperationalStatus.Active));
        dto.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void ToDto_EmploymentContract_MapsEveryField()
    {
        var contract = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), TenantId, Guid.NewGuid(), "Contractual", Today, Today.AddMonths(6),
            true, Guid.NewGuid(), TestEmployment.NowUtc).Value;
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        contract.Renew(Today.AddMonths(6), Today.AddMonths(12), "Approved", TestEmployment.NowUtc);
        contract.Extend(Today.AddMonths(13), "Extended once more", TestEmployment.NowUtc);
        contract.AddDocument("SignedContract", "documents/abc123", 1, TestEmployment.NowUtc);

        var dto = EmploymentMapper.ToDto(contract);

        dto.Id.Should().Be(contract.Id.Value);
        dto.TenantId.Should().Be(contract.TenantId);
        dto.EmploymentId.Should().Be(contract.EmploymentId);
        dto.ContractType.Should().Be("Contractual");
        dto.StartDate.Should().Be(Today.AddMonths(6));
        dto.EndDate.Should().Be(Today.AddMonths(13));
        dto.LifecycleStage.Should().Be(nameof(ContractLifecycleStage.Effective));
        dto.SupersedesContractId.Should().Be(contract.SupersedesContractId);
        dto.CreatedAtUtc.Should().Be(contract.CreatedAtUtc);

        dto.Renewals.Should().HaveCount(1);
        var renewal = dto.Renewals[0];
        renewal.Id.Should().NotBeEmpty();
        renewal.PreviousStartDate.Should().Be(Today);
        renewal.PreviousEndDate.Should().Be(Today.AddMonths(6));
        renewal.NewStartDate.Should().Be(Today.AddMonths(6));
        renewal.NewEndDate.Should().Be(Today.AddMonths(12));
        renewal.ApprovalReference.Should().Be("Approved");

        dto.Extensions.Should().HaveCount(1);
        var extension = dto.Extensions[0];
        extension.Id.Should().NotBeEmpty();
        extension.PreviousEndDate.Should().Be(Today.AddMonths(12));
        extension.NewEndDate.Should().Be(Today.AddMonths(13));
        extension.Reason.Should().Be("Extended once more");

        dto.Documents.Should().HaveCount(1);
        var document = dto.Documents[0];
        document.Id.Should().NotBeEmpty();
        document.DocumentType.Should().Be("SignedContract");
        document.StorageReference.Should().Be("documents/abc123");
        document.Version.Should().Be(1);
    }

    [Fact]
    public void ToDto_EmploymentAssignment_MapsEveryField()
    {
        var employmentId = Guid.NewGuid();
        var firstManagerId = Guid.NewGuid();
        var assignment = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), TenantId, employmentId, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkArrangement.Hybrid, firstManagerId, Today,
            true, TestEmployment.NowUtc).Value;
        var newPositionId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        assignment.ChangePosition(
            newPositionId, newDepartmentId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            WorkArrangement.Remote, MovementType.Lateral, Today.AddDays(30), "Approved", true, TestEmployment.NowUtc);
        var newManagerId = Guid.NewGuid();
        assignment.ChangeReportingManager(newManagerId, false, Today.AddDays(31), TestEmployment.NowUtc);
        assignment.End(Today.AddDays(60), TestEmployment.NowUtc);

        var dto = EmploymentMapper.ToDto(assignment);

        dto.Id.Should().Be(assignment.Id.Value);
        dto.TenantId.Should().Be(assignment.TenantId);
        dto.EmploymentId.Should().Be(employmentId);
        dto.PositionId.Should().Be(newPositionId);
        dto.DepartmentId.Should().Be(newDepartmentId);
        dto.BusinessUnitId.Should().Be(assignment.BusinessUnitId);
        dto.CostCenterId.Should().Be(assignment.CostCenterId);
        dto.WorkLocationId.Should().Be(assignment.WorkLocationId);
        dto.LegalEntityId.Should().Be(assignment.LegalEntityId);
        dto.WorkArrangement.Should().Be(nameof(WorkArrangement.Remote));
        dto.ReportingManagerEmploymentId.Should().Be(newManagerId);
        dto.EffectiveStartDate.Should().Be(Today.AddDays(30));
        dto.IsEnded.Should().BeTrue();
        dto.EndedDate.Should().Be(Today.AddDays(60));
        dto.CreatedAtUtc.Should().Be(assignment.CreatedAtUtc);

        dto.History.Should().HaveCount(2);
        var firstHistory = dto.History[0];
        firstHistory.Id.Should().NotBeEmpty();
        firstHistory.PositionId.Should().NotBeEmpty();
        firstHistory.EffectiveStartDate.Should().Be(Today);
        firstHistory.EffectiveEndDate.Should().Be(Today.AddDays(30));

        dto.ReportingHistory.Should().HaveCount(2);
        var firstReporting = dto.ReportingHistory[0];
        firstReporting.Id.Should().NotBeEmpty();
        firstReporting.ReportingManagerEmploymentId.Should().Be(firstManagerId);
        firstReporting.EffectiveStartDate.Should().Be(Today);
        firstReporting.EffectiveEndDate.Should().Be(Today.AddDays(31));
    }
}
