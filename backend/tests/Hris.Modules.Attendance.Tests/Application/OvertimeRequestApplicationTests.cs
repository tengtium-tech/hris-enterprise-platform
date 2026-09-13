using FluentAssertions;
using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;
using Hris.Modules.Attendance.Application.Queries;
using Hris.Modules.Attendance.Domain;
using Hris.Testing.TenantIsolation;
using MediatR;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Application;

/// <summary>
/// OvertimeRequest's command and query handlers, dispatched through the real MediatR
/// pipeline against the shared tenant-isolation harness (HEP-111).
/// </summary>
public sealed class OvertimeRequestApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    public OvertimeRequestApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private async Task<Guid> SubmitAndReturnIdAsync(Guid? employeeId = null) =>
        (await Sender.Send(new SubmitOvertimeRequestCommand(
            _tenantId, employeeId ?? _employeeId, TestAttendance.Today, new TimeOnly(18, 0), new TimeOnly(20, 0), 2,
            OvertimeCategory.Project, "Month-end close", Guid.NewGuid())).ConfigureAwait(false)).Value;

    [Fact]
    public async Task Submit_CreatesARequestInDraft_RetrievableByQuery()
    {
        var requestId = await SubmitAndReturnIdAsync();

        var result = await Sender.Send(new GetOvertimeRequestQuery(_tenantId, requestId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(ApprovalStatus.Draft));
        result.Value.EstimatedHours.Should().Be(2);
        result.Value.Category.Should().Be(nameof(OvertimeCategory.Project));
    }

    [Fact]
    public async Task Submit_Fails_Validation_WhenEmployeeIdIsEmpty()
    {
        var act = () => Sender.Send(new SubmitOvertimeRequestCommand(
            _tenantId, Guid.Empty, TestAttendance.Today, null, null, 2, OvertimeCategory.Project, "reason", Guid.NewGuid()));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Approve_TransitionsToApproved()
    {
        var requestId = await SubmitAndReturnIdAsync();
        var approverId = Guid.NewGuid();

        var result = await Sender.Send(new ApproveOvertimeRequestCommand(_tenantId, requestId, approverId));

        result.IsSuccess.Should().BeTrue();
        var request = (await Sender.Send(new GetOvertimeRequestQuery(_tenantId, requestId))).Value;
        request.Status.Should().Be(nameof(ApprovalStatus.Approved));
        request.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public async Task Approve_Fails_WhenTheRequestDoesNotExist()
    {
        var result = await Sender.Send(new ApproveOvertimeRequestCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotFound);
    }

    [Fact]
    public async Task Reject_TransitionsToRejected_AndRecordsTheReason()
    {
        var requestId = await SubmitAndReturnIdAsync();

        var result = await Sender.Send(new RejectOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid(), "not justified"));

        result.IsSuccess.Should().BeTrue();
        var request = (await Sender.Send(new GetOvertimeRequestQuery(_tenantId, requestId))).Value;
        request.Status.Should().Be(nameof(ApprovalStatus.Rejected));
        request.RejectionReason.Should().Be("not justified");
    }

    [Fact]
    public async Task Reject_Fails_WhenAlreadyApproved()
    {
        var requestId = await SubmitAndReturnIdAsync();
        await Sender.Send(new ApproveOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid()));

        var result = await Sender.Send(new RejectOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid(), "reason"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotApproved);
    }

    [Fact]
    public async Task Cancel_TransitionsToCancelled_FromDraft()
    {
        var requestId = await SubmitAndReturnIdAsync();

        var result = await Sender.Send(new CancelOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid(), "no longer needed"));

        result.IsSuccess.Should().BeTrue();
        var request = (await Sender.Send(new GetOvertimeRequestQuery(_tenantId, requestId))).Value;
        request.Status.Should().Be(nameof(ApprovalStatus.Cancelled));
    }

    [Fact]
    public async Task Cancel_Fails_WhenAlreadyApproved()
    {
        // Once Approved, an OvertimeRequest has no cancellation path (see OvertimeRequestTests
        // at the Domain layer) -- tested here as the documented current behavior.
        var requestId = await SubmitAndReturnIdAsync();
        await Sender.Send(new ApproveOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid()));

        var result = await Sender.Send(new CancelOvertimeRequestCommand(_tenantId, requestId, Guid.NewGuid(), "reason"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotApproved);
    }

    [Fact]
    public async Task GetPendingOvertimeRequests_ExcludesApprovedRequests_AndScopesToTheGivenTeam()
    {
        var pendingId = await SubmitAndReturnIdAsync();
        var approvedId = await SubmitAndReturnIdAsync();
        await Sender.Send(new ApproveOvertimeRequestCommand(_tenantId, approvedId, Guid.NewGuid()));
        await SubmitAndReturnIdAsync(employeeId: Guid.NewGuid());

        var result = await Sender.Send(new GetPendingOvertimeRequestsQuery(_tenantId, [_employeeId]));

        result.Value.Should().ContainSingle(r => r.Id == pendingId);
    }

    [Fact]
    public async Task GetOvertimeRequestQuery_ReturnsNotFound_ForAnotherTenantsRequest()
    {
        var requestId = await SubmitAndReturnIdAsync();

        var result = await Sender.Send(new GetOvertimeRequestQuery(Guid.NewGuid(), requestId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OvertimeRequestNotFound);
    }
}
