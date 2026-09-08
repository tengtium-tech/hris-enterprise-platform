using FluentAssertions;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Domain;

/// <summary>
/// Exercises every Domain Event record's own properties directly, since most are
/// raised via <c>AddDomainEvent</c> in aggregate tests but never read property-by-
/// property there (aggregate tests mostly assert only <c>DomainEvents.Should().Contain(e
/// =&gt; e is X)</c>). Matches Organization/Position's own established pattern of a
/// dedicated pass closing branch/method coverage gaps left by happy-path tests
/// alone -- here the gap is record-generated property getters, not domain branches.
/// </summary>
public sealed class EmploymentDomainEventsTests
{
    private static readonly Guid _eventId = Guid.NewGuid();
    private static readonly DateTimeOffset _occurredOnUtc = TestEmployment.NowUtc;
    private static readonly DateOnly _today = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
    private static readonly EmploymentId _theEmploymentId = new(Guid.NewGuid());
    private static readonly EmploymentContractId _theContractId = new(Guid.NewGuid());
    private static readonly EmploymentAssignmentId _theAssignmentId = new(Guid.NewGuid());
    private static readonly Guid _employeeId = Guid.NewGuid();

    [Fact]
    public void EmploymentCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var e = new EmploymentCreated(_eventId, _occurredOnUtc, _theEmploymentId, tenantId, _employeeId, "EMP-000001", "Regular", "Rank-and-File");

        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
        e.EmploymentId.Should().Be(_theEmploymentId);
        e.TenantId.Should().Be(tenantId);
        e.EmployeeId.Should().Be(_employeeId);
        e.EmploymentNumber.Should().Be("EMP-000001");
        e.EmploymentType.Should().Be("Regular");
        e.EmploymentCategory.Should().Be("Rank-and-File");
    }

    [Fact]
    public void EmploymentActivated_CarriesAllProperties()
    {
        var e = new EmploymentActivated(_eventId, _occurredOnUtc, _theEmploymentId);

        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentTypeChanged_CarriesAllProperties()
    {
        var e = new EmploymentTypeChanged(_eventId, _occurredOnUtc, _theEmploymentId, "Probationary", "Regular");

        e.PreviousType.Should().Be("Probationary");
        e.NewType.Should().Be("Regular");
        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void EmploymentCategoryChanged_CarriesAllProperties()
    {
        var e = new EmploymentCategoryChanged(_eventId, _occurredOnUtc, _theEmploymentId, "Rank-and-File", "Managerial");

        e.PreviousCategory.Should().Be("Rank-and-File");
        e.NewCategory.Should().Be("Managerial");
        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void EmploymentCompensationChanged_CarriesAllProperties()
    {
        var previous = new CompensationRecordId(Guid.NewGuid());
        var current = new CompensationRecordId(Guid.NewGuid());
        var e = new EmploymentCompensationChanged(
            _eventId, _occurredOnUtc, _theEmploymentId, _employeeId, previous, current, _today, CompensationChangeSource.Hire);

        e.PreviousCompensationRecordId.Should().Be(previous);
        e.NewCompensationRecordId.Should().Be(current);
        e.EffectiveDate.Should().Be(_today);
        e.ChangeSource.Should().Be(CompensationChangeSource.Hire);
        e.EmployeeId.Should().Be(_employeeId);
        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void ProbationStarted_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationStarted(_eventId, _occurredOnUtc, _theEmploymentId, probationId, _today, _today.AddDays(180));

        e.ProbationRecordId.Should().Be(probationId);
        e.StartDate.Should().Be(_today);
        e.ExpectedEvaluationDate.Should().Be(_today.AddDays(180));
        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void ProbationExtended_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationExtended(_eventId, _occurredOnUtc, _theEmploymentId, probationId, _today.AddDays(210));

        e.ProbationRecordId.Should().Be(probationId);
        e.NewExpectedEvaluationDate.Should().Be(_today.AddDays(210));
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentConfirmed_CarriesAllProperties()
    {
        var e = new EmploymentConfirmed(_eventId, _occurredOnUtc, _theEmploymentId);

        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void ProbationFailed_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationFailed(_eventId, _occurredOnUtc, _theEmploymentId, probationId);

        e.ProbationRecordId.Should().Be(probationId);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentSuspended_CarriesAllProperties()
    {
        var e = new EmploymentSuspended(_eventId, _occurredOnUtc, _theEmploymentId, "Investigation", _today);

        e.Reason.Should().Be("Investigation");
        e.EffectiveDate.Should().Be(_today);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentReinstated_CarriesAllProperties()
    {
        var e = new EmploymentReinstated(_eventId, _occurredOnUtc, _theEmploymentId, _today);

        e.EffectiveDate.Should().Be(_today);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentSeconded_CarriesAllProperties()
    {
        var e = new EmploymentSeconded(_eventId, _occurredOnUtc, _theEmploymentId, _today);

        e.EffectiveDate.Should().Be(_today);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmploymentSeparated_CarriesAllProperties()
    {
        var e = new EmploymentSeparated(_eventId, _occurredOnUtc, _theEmploymentId, SeparationType.Resigned, _today);

        e.SeparationReason.Should().Be(SeparationType.Resigned);
        e.EffectiveDate.Should().Be(_today);
        e.EmploymentId.Should().Be(_theEmploymentId);
    }

    [Fact]
    public void EmployeeRehired_CarriesAllProperties()
    {
        var priorId = Guid.NewGuid();
        var e = new EmployeeRehired(_eventId, _occurredOnUtc, _theEmploymentId, _employeeId, priorId, _today);

        e.NewEmploymentId.Should().Be(_theEmploymentId);
        e.EmployeeId.Should().Be(_employeeId);
        e.PriorEmploymentId.Should().Be(priorId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void ConcurrentEmploymentCreated_CarriesAllProperties()
    {
        var primaryId = Guid.NewGuid();
        var e = new ConcurrentEmploymentCreated(_eventId, _occurredOnUtc, _theEmploymentId, _employeeId, primaryId);

        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EmployeeId.Should().Be(_employeeId);
        e.PrimaryEmploymentId.Should().Be(primaryId);
    }

    [Fact]
    public void PrimaryEmploymentChanged_CarriesAllProperties()
    {
        var previousId = Guid.NewGuid();
        var e = new PrimaryEmploymentChanged(_eventId, _occurredOnUtc, _employeeId, _theEmploymentId, previousId);

        e.EmployeeId.Should().Be(_employeeId);
        e.NewPrimaryEmploymentId.Should().Be(_theEmploymentId);
        e.PreviousPrimaryEmploymentId.Should().Be(previousId);
    }

    [Fact]
    public void ConcurrentEmploymentEnded_CarriesAllProperties()
    {
        var e = new ConcurrentEmploymentEnded(_eventId, _occurredOnUtc, _theEmploymentId, _employeeId);

        e.EmploymentId.Should().Be(_theEmploymentId);
        e.EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void EmploymentContractCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var e = new EmploymentContractCreated(_eventId, _occurredOnUtc, _theContractId, tenantId, employmentId, _today, _today.AddYears(1));

        e.EmploymentContractId.Should().Be(_theContractId);
        e.TenantId.Should().Be(tenantId);
        e.EmploymentId.Should().Be(employmentId);
        e.StartDate.Should().Be(_today);
        e.EndDate.Should().Be(_today.AddYears(1));
    }

    [Fact]
    public void EmploymentContractApproved_CarriesAllProperties()
    {
        var e = new EmploymentContractApproved(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
        e.EventId.Should().Be(_eventId);
        e.OccurredOnUtc.Should().Be(_occurredOnUtc);
    }

    [Fact]
    public void EmploymentContractEffective_CarriesAllProperties()
    {
        var e = new EmploymentContractEffective(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractRenewed_CarriesAllProperties()
    {
        var renewalId = new ContractRenewalId(Guid.NewGuid());
        var e = new EmploymentContractRenewed(_eventId, _occurredOnUtc, _theContractId, renewalId, _today, _today.AddYears(1));

        e.ContractRenewalId.Should().Be(renewalId);
        e.NewStartDate.Should().Be(_today);
        e.NewEndDate.Should().Be(_today.AddYears(1));
        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractExtended_CarriesAllProperties()
    {
        var extensionId = new ContractExtensionId(Guid.NewGuid());
        var e = new EmploymentContractExtended(_eventId, _occurredOnUtc, _theContractId, extensionId, _today.AddMonths(9));

        e.ContractExtensionId.Should().Be(extensionId);
        e.NewEndDate.Should().Be(_today.AddMonths(9));
        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractExpired_CarriesAllProperties()
    {
        var e = new EmploymentContractExpired(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractSuperseded_CarriesAllProperties()
    {
        var e = new EmploymentContractSuperseded(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractClosed_CarriesAllProperties()
    {
        var e = new EmploymentContractClosed(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentContractCancelled_CarriesAllProperties()
    {
        var e = new EmploymentContractCancelled(_eventId, _occurredOnUtc, _theContractId);

        e.EmploymentContractId.Should().Be(_theContractId);
    }

    [Fact]
    public void EmploymentAssignmentCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var e = new EmploymentAssignmentCreated(_eventId, _occurredOnUtc, _theAssignmentId, tenantId, employmentId, positionId, _today);

        e.EmploymentAssignmentId.Should().Be(_theAssignmentId);
        e.TenantId.Should().Be(tenantId);
        e.EmploymentId.Should().Be(employmentId);
        e.PositionId.Should().Be(positionId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void EmploymentAssignmentChanged_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentAssignmentChanged(
            _eventId, _occurredOnUtc, _theAssignmentId, employmentId, previousPositionId, newPositionId, _today);

        e.EmploymentAssignmentId.Should().Be(_theAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void EmploymentPromoted_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentPromoted(_eventId, _occurredOnUtc, employmentId, previousPositionId, newPositionId, _today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void EmploymentDemoted_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentDemoted(_eventId, _occurredOnUtc, employmentId, previousPositionId, newPositionId, _today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void EmploymentTransferred_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousUnitId = Guid.NewGuid();
        var newUnitId = Guid.NewGuid();
        var e = new EmploymentTransferred(_eventId, _occurredOnUtc, employmentId, previousUnitId, newUnitId, _today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousOrganizationalUnitId.Should().Be(previousUnitId);
        e.NewOrganizationalUnitId.Should().Be(newUnitId);
        e.EffectiveDate.Should().Be(_today);
    }

    [Fact]
    public void ReportingManagerChanged_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousManagerId = Guid.NewGuid();
        var newManagerId = Guid.NewGuid();
        var e = new ReportingManagerChanged(_eventId, _occurredOnUtc, _theAssignmentId, employmentId, previousManagerId, newManagerId);

        e.EmploymentAssignmentId.Should().Be(_theAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.PreviousReportingManagerEmploymentId.Should().Be(previousManagerId);
        e.NewReportingManagerEmploymentId.Should().Be(newManagerId);
    }

    [Fact]
    public void EmploymentAssignmentEnded_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var e = new EmploymentAssignmentEnded(_eventId, _occurredOnUtc, _theAssignmentId, employmentId, _today);

        e.EmploymentAssignmentId.Should().Be(_theAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.EffectiveDate.Should().Be(_today);
    }
}
