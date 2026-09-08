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
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly DateTimeOffset OccurredOnUtc = TestEmployment.NowUtc;
    private static readonly DateOnly Today = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
    private static readonly EmploymentId TheEmploymentId = new(Guid.NewGuid());
    private static readonly EmploymentContractId TheContractId = new(Guid.NewGuid());
    private static readonly EmploymentAssignmentId TheAssignmentId = new(Guid.NewGuid());
    private static readonly Guid EmployeeId = Guid.NewGuid();

    [Fact]
    public void EmploymentCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var e = new EmploymentCreated(EventId, OccurredOnUtc, TheEmploymentId, tenantId, EmployeeId, "EMP-000001", "Regular", "Rank-and-File");

        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
        e.EmploymentId.Should().Be(TheEmploymentId);
        e.TenantId.Should().Be(tenantId);
        e.EmployeeId.Should().Be(EmployeeId);
        e.EmploymentNumber.Should().Be("EMP-000001");
        e.EmploymentType.Should().Be("Regular");
        e.EmploymentCategory.Should().Be("Rank-and-File");
    }

    [Fact]
    public void EmploymentActivated_CarriesAllProperties()
    {
        var e = new EmploymentActivated(EventId, OccurredOnUtc, TheEmploymentId);

        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentTypeChanged_CarriesAllProperties()
    {
        var e = new EmploymentTypeChanged(EventId, OccurredOnUtc, TheEmploymentId, "Probationary", "Regular");

        e.PreviousType.Should().Be("Probationary");
        e.NewType.Should().Be("Regular");
        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void EmploymentCategoryChanged_CarriesAllProperties()
    {
        var e = new EmploymentCategoryChanged(EventId, OccurredOnUtc, TheEmploymentId, "Rank-and-File", "Managerial");

        e.PreviousCategory.Should().Be("Rank-and-File");
        e.NewCategory.Should().Be("Managerial");
        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void EmploymentCompensationChanged_CarriesAllProperties()
    {
        var previous = new CompensationRecordId(Guid.NewGuid());
        var current = new CompensationRecordId(Guid.NewGuid());
        var e = new EmploymentCompensationChanged(
            EventId, OccurredOnUtc, TheEmploymentId, EmployeeId, previous, current, Today, CompensationChangeSource.Hire);

        e.PreviousCompensationRecordId.Should().Be(previous);
        e.NewCompensationRecordId.Should().Be(current);
        e.EffectiveDate.Should().Be(Today);
        e.ChangeSource.Should().Be(CompensationChangeSource.Hire);
        e.EmployeeId.Should().Be(EmployeeId);
        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void ProbationStarted_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationStarted(EventId, OccurredOnUtc, TheEmploymentId, probationId, Today, Today.AddDays(180));

        e.ProbationRecordId.Should().Be(probationId);
        e.StartDate.Should().Be(Today);
        e.ExpectedEvaluationDate.Should().Be(Today.AddDays(180));
        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void ProbationExtended_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationExtended(EventId, OccurredOnUtc, TheEmploymentId, probationId, Today.AddDays(210));

        e.ProbationRecordId.Should().Be(probationId);
        e.NewExpectedEvaluationDate.Should().Be(Today.AddDays(210));
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentConfirmed_CarriesAllProperties()
    {
        var e = new EmploymentConfirmed(EventId, OccurredOnUtc, TheEmploymentId);

        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void ProbationFailed_CarriesAllProperties()
    {
        var probationId = new ProbationRecordId(Guid.NewGuid());
        var e = new ProbationFailed(EventId, OccurredOnUtc, TheEmploymentId, probationId);

        e.ProbationRecordId.Should().Be(probationId);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentSuspended_CarriesAllProperties()
    {
        var e = new EmploymentSuspended(EventId, OccurredOnUtc, TheEmploymentId, "Investigation", Today);

        e.Reason.Should().Be("Investigation");
        e.EffectiveDate.Should().Be(Today);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentReinstated_CarriesAllProperties()
    {
        var e = new EmploymentReinstated(EventId, OccurredOnUtc, TheEmploymentId, Today);

        e.EffectiveDate.Should().Be(Today);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentSeconded_CarriesAllProperties()
    {
        var e = new EmploymentSeconded(EventId, OccurredOnUtc, TheEmploymentId, Today);

        e.EffectiveDate.Should().Be(Today);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmploymentSeparated_CarriesAllProperties()
    {
        var e = new EmploymentSeparated(EventId, OccurredOnUtc, TheEmploymentId, SeparationType.Resigned, Today);

        e.SeparationReason.Should().Be(SeparationType.Resigned);
        e.EffectiveDate.Should().Be(Today);
        e.EmploymentId.Should().Be(TheEmploymentId);
    }

    [Fact]
    public void EmployeeRehired_CarriesAllProperties()
    {
        var priorId = Guid.NewGuid();
        var e = new EmployeeRehired(EventId, OccurredOnUtc, TheEmploymentId, EmployeeId, priorId, Today);

        e.NewEmploymentId.Should().Be(TheEmploymentId);
        e.EmployeeId.Should().Be(EmployeeId);
        e.PriorEmploymentId.Should().Be(priorId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void ConcurrentEmploymentCreated_CarriesAllProperties()
    {
        var primaryId = Guid.NewGuid();
        var e = new ConcurrentEmploymentCreated(EventId, OccurredOnUtc, TheEmploymentId, EmployeeId, primaryId);

        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EmployeeId.Should().Be(EmployeeId);
        e.PrimaryEmploymentId.Should().Be(primaryId);
    }

    [Fact]
    public void PrimaryEmploymentChanged_CarriesAllProperties()
    {
        var previousId = Guid.NewGuid();
        var e = new PrimaryEmploymentChanged(EventId, OccurredOnUtc, EmployeeId, TheEmploymentId, previousId);

        e.EmployeeId.Should().Be(EmployeeId);
        e.NewPrimaryEmploymentId.Should().Be(TheEmploymentId);
        e.PreviousPrimaryEmploymentId.Should().Be(previousId);
    }

    [Fact]
    public void ConcurrentEmploymentEnded_CarriesAllProperties()
    {
        var e = new ConcurrentEmploymentEnded(EventId, OccurredOnUtc, TheEmploymentId, EmployeeId);

        e.EmploymentId.Should().Be(TheEmploymentId);
        e.EmployeeId.Should().Be(EmployeeId);
    }

    [Fact]
    public void EmploymentContractCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var e = new EmploymentContractCreated(EventId, OccurredOnUtc, TheContractId, tenantId, employmentId, Today, Today.AddYears(1));

        e.EmploymentContractId.Should().Be(TheContractId);
        e.TenantId.Should().Be(tenantId);
        e.EmploymentId.Should().Be(employmentId);
        e.StartDate.Should().Be(Today);
        e.EndDate.Should().Be(Today.AddYears(1));
    }

    [Fact]
    public void EmploymentContractApproved_CarriesAllProperties()
    {
        var e = new EmploymentContractApproved(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
        e.EventId.Should().Be(EventId);
        e.OccurredOnUtc.Should().Be(OccurredOnUtc);
    }

    [Fact]
    public void EmploymentContractEffective_CarriesAllProperties()
    {
        var e = new EmploymentContractEffective(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractRenewed_CarriesAllProperties()
    {
        var renewalId = new ContractRenewalId(Guid.NewGuid());
        var e = new EmploymentContractRenewed(EventId, OccurredOnUtc, TheContractId, renewalId, Today, Today.AddYears(1));

        e.ContractRenewalId.Should().Be(renewalId);
        e.NewStartDate.Should().Be(Today);
        e.NewEndDate.Should().Be(Today.AddYears(1));
        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractExtended_CarriesAllProperties()
    {
        var extensionId = new ContractExtensionId(Guid.NewGuid());
        var e = new EmploymentContractExtended(EventId, OccurredOnUtc, TheContractId, extensionId, Today.AddMonths(9));

        e.ContractExtensionId.Should().Be(extensionId);
        e.NewEndDate.Should().Be(Today.AddMonths(9));
        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractExpired_CarriesAllProperties()
    {
        var e = new EmploymentContractExpired(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractSuperseded_CarriesAllProperties()
    {
        var e = new EmploymentContractSuperseded(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractClosed_CarriesAllProperties()
    {
        var e = new EmploymentContractClosed(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentContractCancelled_CarriesAllProperties()
    {
        var e = new EmploymentContractCancelled(EventId, OccurredOnUtc, TheContractId);

        e.EmploymentContractId.Should().Be(TheContractId);
    }

    [Fact]
    public void EmploymentAssignmentCreated_CarriesAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var e = new EmploymentAssignmentCreated(EventId, OccurredOnUtc, TheAssignmentId, tenantId, employmentId, positionId, Today);

        e.EmploymentAssignmentId.Should().Be(TheAssignmentId);
        e.TenantId.Should().Be(tenantId);
        e.EmploymentId.Should().Be(employmentId);
        e.PositionId.Should().Be(positionId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void EmploymentAssignmentChanged_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentAssignmentChanged(
            EventId, OccurredOnUtc, TheAssignmentId, employmentId, previousPositionId, newPositionId, Today);

        e.EmploymentAssignmentId.Should().Be(TheAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void EmploymentPromoted_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentPromoted(EventId, OccurredOnUtc, employmentId, previousPositionId, newPositionId, Today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void EmploymentDemoted_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousPositionId = Guid.NewGuid();
        var newPositionId = Guid.NewGuid();
        var e = new EmploymentDemoted(EventId, OccurredOnUtc, employmentId, previousPositionId, newPositionId, Today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousPositionId.Should().Be(previousPositionId);
        e.NewPositionId.Should().Be(newPositionId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void EmploymentTransferred_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousUnitId = Guid.NewGuid();
        var newUnitId = Guid.NewGuid();
        var e = new EmploymentTransferred(EventId, OccurredOnUtc, employmentId, previousUnitId, newUnitId, Today);

        e.EmploymentId.Should().Be(employmentId);
        e.PreviousOrganizationalUnitId.Should().Be(previousUnitId);
        e.NewOrganizationalUnitId.Should().Be(newUnitId);
        e.EffectiveDate.Should().Be(Today);
    }

    [Fact]
    public void ReportingManagerChanged_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var previousManagerId = Guid.NewGuid();
        var newManagerId = Guid.NewGuid();
        var e = new ReportingManagerChanged(EventId, OccurredOnUtc, TheAssignmentId, employmentId, previousManagerId, newManagerId);

        e.EmploymentAssignmentId.Should().Be(TheAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.PreviousReportingManagerEmploymentId.Should().Be(previousManagerId);
        e.NewReportingManagerEmploymentId.Should().Be(newManagerId);
    }

    [Fact]
    public void EmploymentAssignmentEnded_CarriesAllProperties()
    {
        var employmentId = Guid.NewGuid();
        var e = new EmploymentAssignmentEnded(EventId, OccurredOnUtc, TheAssignmentId, employmentId, Today);

        e.EmploymentAssignmentId.Should().Be(TheAssignmentId);
        e.EmploymentId.Should().Be(employmentId);
        e.EffectiveDate.Should().Be(Today);
    }
}
