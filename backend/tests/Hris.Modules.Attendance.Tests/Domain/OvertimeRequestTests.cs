using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// AT-040, AT-041: one pre-authorization request, standing apart from
/// <see cref="AttendanceRecord"/> so it can exist before any attendance does. There is
/// no explicit submission step reachable through the public API beyond
/// <see cref="OvertimeRequest.Create"/> itself, which starts every request in
/// <see cref="ApprovalStatus.Draft"/> — Approve, Reject, and Cancel are all callable
/// directly from that state.
/// </summary>
public sealed class OvertimeRequestTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    private OvertimeRequest NewRequest() =>
        OvertimeRequest.Create(
            new OvertimeRequestId(Guid.NewGuid()), _tenantId, _employeeId, TestAttendance.Today,
            new TimeOnly(18, 0), new TimeOnly(20, 0), 2, OvertimeCategory.Project, "Month-end close",
            Guid.NewGuid(), TestAttendance.NowUtc).Value;

    [Fact]
    public void Create_StartsInDraft_AndRaisesSubmittedEvent()
    {
        var request = NewRequest();

        request.Status.Should().Be(ApprovalStatus.Draft);
        request.DomainEvents.OfType<OvertimeRequestSubmitted>().Single().EmployeeId.Should().Be(_employeeId);
    }

    [Fact]
    public void Create_Fails_WhenEmployeeIdIsEmpty()
    {
        var result = OvertimeRequest.Create(
            new OvertimeRequestId(Guid.NewGuid()), _tenantId, Guid.Empty, TestAttendance.Today, null, null, 2,
            OvertimeCategory.Project, "reason", Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.EmployeeIdentifierRequired);
    }

    [Fact]
    public void Approve_Succeeds_FromDraft_AndRaisesApprovedEvent()
    {
        var request = NewRequest();
        var approverId = Guid.NewGuid();

        var result = request.Approve(approverId, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ApprovalStatus.Approved);
        request.ApproverId.Should().Be(approverId);
        request.DomainEvents.OfType<OvertimeRequestApproved>().Single().ApproverId.Should().Be(approverId);
    }

    [Fact]
    public void Approve_IsIdempotent_WhenAlreadyApproved()
    {
        var request = NewRequest();
        request.Approve(Guid.NewGuid(), TestAttendance.NowUtc);
        request.ClearDomainEvents();

        var result = request.Approve(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Cancelled)]
    public void Approve_Fails_WhenRejectedOrCancelled(ApprovalStatus terminalStatus)
    {
        var request = NewRequest();
        MoveTo(request, terminalStatus);

        var result = request.Approve(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotApproved);
    }

    [Fact]
    public void Reject_Succeeds_FromDraft_AndRecordsTheReason()
    {
        var request = NewRequest();

        var result = request.Reject(Guid.NewGuid(), "not justified", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ApprovalStatus.Rejected);
        request.RejectionReason.Should().Be("not justified");
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Cancelled)]
    public void Reject_Fails_WhenApprovedRejectedOrCancelled(ApprovalStatus terminalStatus)
    {
        var request = NewRequest();
        MoveTo(request, terminalStatus);

        var result = request.Reject(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotApproved);
    }

    [Fact]
    public void Cancel_Succeeds_FromDraft()
    {
        var request = NewRequest();

        var result = request.Cancel(Guid.NewGuid(), "no longer needed", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ApprovalStatus.Cancelled);
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Cancelled)]
    public void Cancel_Fails_WhenApprovedOrAlreadyCancelled(ApprovalStatus terminalStatus)
    {
        // Once Approved, an OvertimeRequest has no cancellation path -- unlike
        // AttendanceAdjustment, which permits Cancel even from Approved. Tested as the
        // documented current behavior, not asserted as correct or incorrect design.
        var request = NewRequest();
        MoveTo(request, terminalStatus);

        var result = request.Cancel(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotApproved);
    }

    private static void MoveTo(OvertimeRequest request, ApprovalStatus status)
    {
        switch (status)
        {
            case ApprovalStatus.Approved:
                request.Approve(Guid.NewGuid(), TestAttendance.NowUtc);
                break;
            case ApprovalStatus.Rejected:
                request.Reject(Guid.NewGuid(), "n", TestAttendance.NowUtc);
                break;
            case ApprovalStatus.Cancelled:
                request.Cancel(Guid.NewGuid(), "n", TestAttendance.NowUtc);
                break;
        }
    }
}
