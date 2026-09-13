using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-030 through LV-041: the module's primary transactional root, carrying an employee's
/// request through approval, rejection, and cancellation without ever directly writing to
/// <c>LeaveBalance</c> — every balance effect is triggered by an event and applied
/// separately (LV-040, LV-041).
/// </summary>
public sealed class LeaveRequestTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly LeaveTypeId _leaveTypeId = new(Guid.NewGuid());

    private static LeaveDateRange DefaultRange() =>
        new(TestLeave.Today, TestLeave.Today.AddDays(2), false, false, 3);

    private LeaveRequest NewRequest(LeaveDateRange? dateRange = null) =>
        LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId, dateRange ?? DefaultRange(), null,
            requiresStatutoryDetails: false, null, Guid.NewGuid(), TestLeave.NowUtc).Value;

    private static ApprovalDecision ApprovedDecision(Guid approverId) =>
        new(approverId, ApprovalDecisionOutcome.Approved, TestLeave.NowUtc, null, null, null);

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsPendingApproval_AndRaisesRequestedEvent()
    {
        var submittedBy = Guid.NewGuid();
        var result = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId, DefaultRange(), null,
            requiresStatutoryDetails: false, null, submittedBy, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var request = result.Value;
        request.Status.Should().Be(LeaveRequestStatus.PendingApproval);
        request.PayTreatment.Should().BeNull("LV-037: pay treatment is finalized only at approval");
        request.DomainEvents.OfType<LeaveRequested>().Should().ContainSingle().Which.SubmittedBy.Should().Be(submittedBy);
    }

    [Fact]
    public void Create_Fails_WhenEmployeeIdIsEmpty()
    {
        var result = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, Guid.Empty, _leaveTypeId, DefaultRange(), null,
            requiresStatutoryDetails: false, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EmployeeIdentifierRequired);
    }

    [Fact]
    public void Create_Fails_WhenEndDateIsBeforeStartDate()
    {
        var invalidRange = new LeaveDateRange(TestLeave.Today, TestLeave.Today.AddDays(-1), false, false, 1);

        var result = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId, invalidRange, null,
            requiresStatutoryDetails: false, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveDateRangeInvalid);
    }

    [Fact]
    public void Create_Fails_WhenStatutoryDetailsAreRequiredButMissing()
    {
        var result = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId, DefaultRange(), null,
            requiresStatutoryDetails: true, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.StatutoryDetailsRequired);
    }

    [Fact]
    public void Create_Succeeds_WhenStatutoryDetailsAreRequiredAndProvided()
    {
        var details = new StatutoryDetails(StatutoryDetailsVariant.Paternity, null, null, null, null, true, true, null, null, null);

        var result = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId, DefaultRange(), details,
            requiresStatutoryDetails: true, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.StatutoryDetails.Should().Be(details);
    }

    // ---- Approve (LV-037, LV-040, LV-086) ---------------------------------

    [Fact]
    public void Approve_Succeeds_AndFinalizesPayTreatmentAndPaidDays()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();

        var result = request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.Paid, 3m, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Approved);
        request.PayTreatment.Should().Be(PayTreatment.Paid);
        request.PaidDays.Should().Be(3m);
        request.Decision!.ApproverId.Should().Be(approverId);

        var raised = request.DomainEvents.OfType<LeaveApproved>().Single();
        raised.PayTreatment.Should().Be(PayTreatment.Paid);
        raised.PaidDays.Should().Be(3m);
    }

    [Fact]
    public void Approve_RaisesLWOPPeriodRecorded_WhenPayTreatmentIsNotFullyPaid()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();

        request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.PartiallyPaid, 1m, TestLeave.NowUtc);

        request.DomainEvents.OfType<LWOPPeriodRecorded>().Should().ContainSingle();
    }

    [Fact]
    public void Approve_DoesNotRaiseLWOPPeriodRecorded_WhenFullyPaid()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();

        request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.Paid, 3m, TestLeave.NowUtc);

        request.DomainEvents.OfType<LWOPPeriodRecorded>().Should().BeEmpty();
    }

    [Fact]
    public void Approve_Fails_WhenNotPendingApproval()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();
        request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.Paid, 3m, TestLeave.NowUtc);

        var result = request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.Paid, 3m, TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveRequestNotPendingApproval);
    }

    // ---- Reject --------------------------------------------------------

    [Fact]
    public void Reject_Succeeds_AndRecordsTheReason()
    {
        var request = NewRequest();

        var result = request.Reject(Guid.NewGuid(), "insufficient coverage", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Rejected);
        request.RejectionReason.Should().Be("insufficient coverage");
        request.DomainEvents.OfType<LeaveRejected>().Should().ContainSingle();
    }

    [Fact]
    public void Reject_Fails_WhenNotPendingApproval()
    {
        var request = NewRequest();
        request.Reject(Guid.NewGuid(), "first", TestLeave.NowUtc);

        var result = request.Reject(Guid.NewGuid(), "second", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveRequestNotPendingApproval);
    }

    // ---- Cancel (LV-041) -----------------------------------------------

    [Fact]
    public void Cancel_Succeeds_FromPendingApproval_WithWasApprovedFalse()
    {
        var request = NewRequest();

        var result = request.Cancel(Guid.NewGuid(), "no longer needed", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Cancelled);
        var raised = request.DomainEvents.OfType<LeaveCancelled>().Single();
        raised.WasApproved.Should().BeFalse("no deduction was ever applied, so none needs reversing");
        raised.PaidDaysToRestore.Should().Be(0);
    }

    [Fact]
    public void Cancel_Succeeds_AfterApproval_WithWasApprovedTrue_AndCarriesThePaidDaysToRestore()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();
        request.Approve(approverId, ApprovedDecision(approverId), PayTreatment.PartiallyPaid, 2m, TestLeave.NowUtc);

        var result = request.Cancel(Guid.NewGuid(), "changed plans", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var raised = request.DomainEvents.OfType<LeaveCancelled>().Single();
        raised.WasApproved.Should().BeTrue();
        raised.PaidDaysToRestore.Should().Be(2m, "only the portion actually deducted (PartiallyPaid) needs restoring, not the full requested amount");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_Fails_WhenAlreadyTerminal(bool wasRejectedRatherThanCancelled)
    {
        var request = NewRequest();
        if (wasRejectedRatherThanCancelled)
        {
            request.Reject(Guid.NewGuid(), "n", TestLeave.NowUtc);
        }
        else
        {
            request.Cancel(Guid.NewGuid(), "n", TestLeave.NowUtc);
        }

        var result = request.Cancel(Guid.NewGuid(), "again", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveRequestNotCancellable);
    }
}
