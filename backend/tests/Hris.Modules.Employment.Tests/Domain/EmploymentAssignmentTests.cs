using FluentAssertions;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Domain;

public sealed class EmploymentAssignmentTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateOnly _effectiveDate = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

    [Fact]
    public void Create_WithActivePosition_Succeeds()
    {
        var employmentId = Guid.NewGuid();

        var result = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), _tenantId, employmentId, Guid.NewGuid(), Guid.NewGuid(), null,
            null, null, null, WorkArrangement.Remote, null, _effectiveDate, true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnded.Should().BeFalse();
    }

    [Fact]
    public void Create_WithInactivePosition_Fails()
    {
        var result = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, null, null,
            null, null, WorkArrangement.OnSite, null, _effectiveDate, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.AssignmentRequiresActivePosition);
    }

    [Fact]
    public void Create_ReportingToSelf_Fails()
    {
        var employmentId = Guid.NewGuid();

        var result = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), _tenantId, employmentId, Guid.NewGuid(), null, null, null, null,
            null, WorkArrangement.OnSite, employmentId, _effectiveDate, true, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.SelfReportingProhibited);
    }

    [Fact]
    public void Create_WithReportingManager_RecordsInitialReportingAssignment()
    {
        var managerId = Guid.NewGuid();

        var result = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, null, null,
            null, null, WorkArrangement.OnSite, managerId, _effectiveDate, true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportingHistory.Should().HaveCount(1);
        result.Value.ReportingManagerEmploymentId.Should().Be(managerId);
    }

    [Fact]
    public void ChangePosition_Lateral_UpdatesPositionAndArchivesHistory()
    {
        var employmentId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(_tenantId, employmentId);
        var previousPositionId = assignment.PositionId;
        var newPositionId = Guid.NewGuid();

        var result = assignment.ChangePosition(
            newPositionId, Guid.NewGuid(), null, null, null, null, WorkArrangement.Hybrid, MovementType.Lateral,
            _effectiveDate.AddDays(30), null, true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.PositionId.Should().Be(newPositionId);
        assignment.History.Should().HaveCount(1);
        assignment.History[0].PositionId.Should().Be(previousPositionId);
        assignment.DomainEvents.Should().Contain(e => e is EmploymentAssignmentChanged);
        assignment.DomainEvents.Should().Contain(e => e is EmploymentTransferred);
    }

    [Fact]
    public void ChangePosition_Promotion_RaisesEmploymentPromoted()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());

        var result = assignment.ChangePosition(
            Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite, MovementType.Promotion,
            _effectiveDate.AddDays(30), "Approved", true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.DomainEvents.Should().Contain(e => e is EmploymentPromoted);
    }

    [Fact]
    public void ChangePosition_Demotion_RaisesEmploymentDemoted()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());

        var result = assignment.ChangePosition(
            Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite, MovementType.Demotion,
            _effectiveDate.AddDays(30), "Approved", true, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.DomainEvents.Should().Contain(e => e is EmploymentDemoted);
    }

    [Fact]
    public void ChangePosition_WithInactivePosition_Fails()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());

        var result = assignment.ChangePosition(
            Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite, MovementType.Lateral,
            _effectiveDate.AddDays(30), null, false, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.AssignmentRequiresActivePosition);
    }

    [Fact]
    public void ChangePosition_WhenEnded_Fails()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());
        assignment.End(_effectiveDate, TestEmployment.NowUtc);

        var result = assignment.ChangePosition(
            Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite, MovementType.Lateral,
            _effectiveDate.AddDays(30), null, true, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.AssignmentAlreadyEnded);
    }

    [Fact]
    public void ChangeReportingManager_WithValidManager_Succeeds()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());
        var newManagerId = Guid.NewGuid();

        var result = assignment.ChangeReportingManager(newManagerId, false, _effectiveDate.AddDays(1), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.ReportingManagerEmploymentId.Should().Be(newManagerId);
        assignment.ReportingHistory.Should().HaveCount(1);
    }

    [Fact]
    public void ChangeReportingManager_ToSelf_Fails()
    {
        var employmentId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(_tenantId, employmentId);

        var result = assignment.ChangeReportingManager(employmentId, false, _effectiveDate.AddDays(1), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.SelfReportingProhibited);
    }

    [Fact]
    public void ChangeReportingManager_WouldCreateCircularReporting_Fails()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());

        var result = assignment.ChangeReportingManager(Guid.NewGuid(), true, _effectiveDate.AddDays(1), TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CircularReportingProhibited);
    }

    [Fact]
    public void ChangeReportingManager_ClosesExistingReportingAssignment()
    {
        var assignment = EmploymentAssignment.Create(
            new EmploymentAssignmentId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, null, null,
            null, null, WorkArrangement.OnSite, Guid.NewGuid(), _effectiveDate, true, TestEmployment.NowUtc).Value;

        assignment.ChangeReportingManager(Guid.NewGuid(), false, _effectiveDate.AddDays(1), TestEmployment.NowUtc);

        assignment.ReportingHistory.Should().HaveCount(2);
        assignment.ReportingHistory[0].EffectiveEndDate.Should().NotBeNull();
    }

    [Fact]
    public void End_WhenNotEnded_Succeeds()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());

        var result = assignment.End(_effectiveDate.AddDays(10), TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.IsEnded.Should().BeTrue();
        assignment.History.Should().HaveCount(1);
    }

    [Fact]
    public void End_WhenAlreadyEnded_Fails()
    {
        var assignment = TestEmployment.CreateAssignment(_tenantId, Guid.NewGuid());
        assignment.End(_effectiveDate, TestEmployment.NowUtc);

        var result = assignment.End(_effectiveDate, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.AssignmentAlreadyEnded);
    }
}
